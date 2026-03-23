using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
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
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateOrderHandler> _logger;

        public CreateOrderHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<CreateOrderHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
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

            // Validate Basic Logic for dine-in
            if (request.OrderType == OrderType.DineIn)
            {
                if (request.TableId == null && request.ReservationId == null)
                {
                    return Result<Guid>.Failure(
                        _messageService.GetMessage(MessageKeys.Order.SelectTable),
                        ResultErrorType.BadRequest
                    );
                }
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

                    // Table must be Available (or we could enforce checking the reservation status too, like Booked/CheckIn)
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

                // If table not from reservation, try fetch by TableId
                if (
                    table == null
                    && request.OrderType == OrderType.DineIn
                    && request.TableId.HasValue
                )
                {
                    table = await _unitOfWork
                        .Repository<Table>()
                        .Query()
                        .Include(t => t.Area)
                        .FirstOrDefaultAsync(
                            t => t.TableId == request.TableId.Value,
                            cancellationToken
                        );

                    if (table is null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<Guid>.Failure(
                            _messageService.GetMessage(MessageKeys.Table.NotFound),
                            ResultErrorType.NotFound
                        );
                    }

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

                    if (table.Area!.Status == AreaStatus.Inactive)
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
                    TableId = request.OrderType == OrderType.DineIn ? table?.TableId : null,
                    ReservationId =
                        request.OrderType == OrderType.DineIn ? request.ReservationId : null,
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
                await _cacheService.RemoveByPatternAsync(
                    CacheKey.TableList + "*",
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync(
                    string.Format(CacheKey.TableListByArea, "*"),
                    cancellationToken
                );
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
