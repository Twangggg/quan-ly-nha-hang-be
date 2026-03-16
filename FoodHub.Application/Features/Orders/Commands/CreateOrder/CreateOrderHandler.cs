using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
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
            var userId = _currentUserService.GetUserIdAsGuid();
            if (userId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Creating new order. Type: {OrderType}, Reservation: {ReservationId}, CreatedBy: {UserId}",
                request.OrderType,
                request.ReservationId,
                userId
            );

            // Validate Basic Logic
            if (request.OrderType == OrderType.DineIn && request.ReservationId == null)
            {
                return Result<Guid>.Failure(
                    _messageService.GetMessage(MessageKeys.Order.SelectTable), // You may want to create a new message key for this later if needed like "SelectReservation"
                    ResultErrorType.BadRequest
                );
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservationRepository = _unitOfWork.Repository<Reservation>();

                Reservation? reservation = null;
                Table? table = null;
                if (request.OrderType == OrderType.DineIn && request.ReservationId.HasValue)
                {
                    // Load reservation + table + area in one query
                    reservation = await reservationRepository
                        .Query()
                        .Include(r => r.Table)
                        .ThenInclude(t => t.Area)
                        .FirstOrDefaultAsync(
                            r => r.ReservationId == request.ReservationId.Value,
                            cancellationToken
                        );

                    if (reservation is null)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return Result<Guid>.Failure(
                            _messageService.GetMessage(MessageKeys.Reservation.NotFound),
                            ResultErrorType.NotFound
                        );
                    }

                    table = reservation.Table;

                    // Table must be Available (or we could enforce checking the reservation status too, like Booked/CheckIn)
                    // Depending on your requirements, if a reservation is checked in, maybe the table is already Occupied,
                    // but according to previous logic, table must be Available.
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
                    ReservationId = request.OrderType == OrderType.DineIn ? request.ReservationId : null,
                    Note = request.Note,
                    TotalAmount = 0,
                    IsPriority = isPriority,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId.Value,
                };

                await _unitOfWork.Repository<Order>().AddAsync(newOrder);

                var auditLog = OrderAuditLog.CreateOrderCreated(
                    newOrder.OrderId,
                    userId.Value,
                    newOrder.OrderCode,
                    newOrder.OrderType,
                    newOrder.TableId
                );
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
                    "Database error while creating order. Type: {OrderType}, Reservation: {ReservationId}",
                    request.OrderType,
                    request.ReservationId
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
                    "Unexpected error while creating order. Type: {OrderType}, Reservation: {ReservationId}",
                    request.OrderType,
                    request.ReservationId
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
