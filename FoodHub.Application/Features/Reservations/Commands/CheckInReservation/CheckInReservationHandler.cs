using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.CheckInReservation
{
    public class CheckInReservationHandler
        : IRequestHandler<CheckInReservationCommand, Result<CheckInReservationResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CheckInReservationHandler> _logger;

        public CheckInReservationHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<CheckInReservationHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<CheckInReservationResponse>> Handle(
            CheckInReservationCommand request,
            CancellationToken cancellationToken
        )
        {
            // 1. Validate user login
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Unauthorized
                );
            }

            _logger.LogInformation(
                "Check-in reservation {ReservationId} by user {UserId}",
                request.ReservationId,
                userId
            );

            // 2. Find reservation with table + area
            var reservation = await _unitOfWork
                .Repository<Reservation>()
                .Query()
                .Include(r => r.Table)
                    .ThenInclude(t => t.Area)
                .FirstOrDefaultAsync(
                    r => r.ReservationId == request.ReservationId,
                    cancellationToken
                );

            if (reservation is null)
            {
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Reservation.NotFound),
                    ResultErrorType.NotFound
                );
            }

            // 3. Validate reservation status (must be Booked)
            if (reservation.Status != ReservationStatus.Booked)
            {
                _logger.LogWarning(
                    "Cannot check-in reservation {ReservationId} — Status is {Status}",
                    reservation.ReservationId,
                    reservation.Status
                );
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Reservation.InvalidStatusForCheckIn),
                    ResultErrorType.BadRequest
                );
            }

            // 4. Validate table availability (must be Available or Reserved)
            var table = reservation.Table;
            if (table.Status != TableStatus.Available && table.Status != TableStatus.Reserved)
            {
                _logger.LogWarning(
                    "Cannot check-in — Table {TableId} is {Status}",
                    table.TableId,
                    table.Status
                );
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Reservation.TableOccupied),
                    ResultErrorType.Conflict
                );
            }

            // 5. Prevent duplicate: check no existing Order linked to this Reservation
            var alreadyHasOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AnyAsync(
                    o => o.ReservationId == request.ReservationId,
                    cancellationToken
                );

            if (alreadyHasOrder)
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} already has an Order",
                    request.ReservationId
                );
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Reservation.AlreadyCheckedIn),
                    ResultErrorType.Conflict
                );
            }

            // 6. Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Generate order code
                var orderCode = await GenerateOrderCodeAsync(cancellationToken);

                // IsPriority = true if table belongs to VIP area
                var isPriority = table.Area?.Type == AreaType.VIP;

                // Create Order
                var newOrder = new Order
                {
                    OrderId = Guid.NewGuid(),
                    OrderCode = orderCode,
                    OrderType = OrderType.DineIn,
                    Status = OrderStatus.Serving,
                    TableId = table.TableId,
                    ReservationId = reservation.ReservationId,
                    Note = reservation.Note,
                    TotalAmount = 0,
                    IsPriority = isPriority,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                };

                await _unitOfWork.Repository<Order>().AddAsync(newOrder);

                // Create audit log
                var auditLog = new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = newOrder.OrderId,
                    EmployeeId = userId,
                    Action = AuditLogActions.CheckInReservation,
                    NewValue =
                        $"{{\"orderCode\": \"{newOrder.OrderCode}\", \"reservationId\": \"{reservation.ReservationId}\", \"tableId\": \"{table.TableId}\"}}",
                    CreatedAt = DateTime.UtcNow,
                };
                await _unitOfWork.Repository<OrderAuditLog>().AddAsync(auditLog);

                // Update reservation status → CheckIn
                reservation.Status = ReservationStatus.CheckIn;
                reservation.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Reservation>().Update(reservation);

                // Update table status → Occupied
                table.Status = TableStatus.Occupied;
                _unitOfWork.Repository<Table>().Update(table);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully checked-in reservation {ReservationId} → Order {OrderCode} (Id: {OrderId})",
                    reservation.ReservationId,
                    newOrder.OrderCode,
                    newOrder.OrderId
                );

                return Result<CheckInReservationResponse>.Success(
                    new CheckInReservationResponse
                    {
                        OrderId = newOrder.OrderId,
                        OrderCode = newOrder.OrderCode,
                    }
                );
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Database error during check-in for reservation {ReservationId}",
                    request.ReservationId
                );
                return Result<CheckInReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.Conflict
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Unexpected error during check-in for reservation {ReservationId}",
                    request.ReservationId
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
