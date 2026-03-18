using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateOrderHandler> _logger;

        public CreateOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CreateOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Creating new order. Type: {OrderType}, Table: {TableId}, CreatedBy: {UserId}",
                request.OrderType,
                request.TableId,
                userId
            );

            // Validate Basic Logic
            if (request.OrderType == OrderType.DineIn && request.TableId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.SelectTable),
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tableRepository = _unitOfWork.Repository<Table>();

                Table? table = null;
                if (request.OrderType == OrderType.DineIn && request.TableId.HasValue)
                {
                    // Load table + area in one query
                    table = await tableRepository
                        .Query()
                        .Include(t => t.Area)
                        .FirstOrDefaultAsync(
                            t => t.TableId == request.TableId.Value,
                            cancellationToken
                        );

                    var bufferTime = TimeSpan.FromHours(2);
                    var now = DateTime.Now;
                    var currentTime = now.TimeOfDay;
                    var today = DateOnly.FromDateTime(now);
                    var upcomingReservation = await _unitOfWork.Repository<Reservation>().Query()
                        .AnyAsync(r => r.TableId == request.TableId.Value
                                    && r.ReservationDate == today
                                    && r.Status == ReservationStatus.Booked
                                    && r.ReservationTime > currentTime
                                    && r.ReservationTime <= currentTime.Add(bufferTime),
                                    cancellationToken);

                    if (table is null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<Guid>.Failure(
                            _messageService.GetMessage(MessageKeys.Table.NotFound),
                            ResultErrorType.NotFound
                        );
                    }

                    // Bàn phải ở trạng thái Available mới được tạo order
                    if (table.Status != TableStatus.Available)
                    {
                        _logger.LogWarning(
                            "Cannot create order — Table {TableId} is not Available (Status: {Status})",
                            table.TableId,
                            table.Status
                        );
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<Guid>.Failure(
                            _messageService.GetMessage(MessageKeys.Table.NotAvailable),
                            ResultErrorType.Conflict
                        );
                    }

                    // Khu vực phải Active mới cho tạo order
                    if (table.Area.Status == AreaStatus.Inactive)
                    {
                        _logger.LogWarning(
                            "Cannot create order — Area {AreaId} for Table {TableId} is Inactive",
                            table.AreaId,
                            table.TableId
                        );
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<Guid>.Failure(
                            _messageService.GetMessage(MessageKeys.Area.Inactive),
                            ResultErrorType.Conflict
                        );
                    }

                    //Bàn bị đặt trước
                    if (upcomingReservation)
                    {
                        return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Order.HasBeenPlaced));
                    }
                }

                // Generate Order Code inside transaction to prevent race condition
                var orderCode = await GenerateOrderCodeAsync(cancellationToken);

                // IsPriority = true nếu bàn thuộc khu VIP
                var isPriority = table?.Area?.Type == AreaType.VIP;

                // Create Order
                var newOrder = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = orderCode,
                    OrderType = request.OrderType,
                    Status = OrderStatus.Serving,
                    TableId = request.OrderType == OrderType.DineIn ? request.TableId : null,
                    Note = request.Note,
                    TotalAmount = 0,
                    IsPriority = isPriority,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                };

                await _unitOfWork.Repository<Order>().AddAsync(newOrder);

                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = newOrder.OrderId,
                    EmployeeId = userId,
                    Action = AuditLogActions.CreateOrder,
                    NewValue =
                        $"{{\"orderCode\": \"{newOrder.OrderCode}\", \"orderType\": \"{newOrder.OrderType}\", \"tableId\": \"{newOrder.TableId}\"}}",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                // Cập nhật trạng thái bàn sang Occupied
                if (table != null)
                {
                    table.Status = TableStatus.Occupied;
                    _unitOfWork.Repository<Table>().Update(table);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully created order {OrderCode} (Id: {OrderId})",
                    newOrder.OrderCode,
                    newOrder.OrderId
                );

                return Result<Guid>.Success(newOrder.OrderId);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error while creating order. Type: {OrderType}, Table: {TableId}",
                    request.OrderType,
                    request.TableId
                );
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error while creating order. Type: {OrderType}, Table: {TableId}",
                    request.OrderType,
                    request.TableId
                );
                throw;
            }
        }

        /// <summary>
        /// Generate unique order code in format: ORD-yyyyMMdd-xxxx.
        /// Must be called inside a transaction to prevent race conditions.
        /// </summary>
        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";

            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            int sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }

            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}
