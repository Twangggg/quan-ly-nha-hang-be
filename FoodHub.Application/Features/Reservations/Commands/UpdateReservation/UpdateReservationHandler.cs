using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.UpdateReservation
{
    public class UpdateReservationHandler : IRequestHandler<UpdateReservationCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateReservationHandler> _logger;
        private readonly IMessageService _messageService;

        public UpdateReservationHandler(IUnitOfWork unitOfWork, ILogger<UpdateReservationHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating reservation {ReservationId} for {CustomerName}", request.ReservationId, request.CustomerName);

            var reservation = await _unitOfWork.Repository<Reservation>().Query()
                .FirstOrDefaultAsync(r => r.ReservationId == request.ReservationId, cancellationToken);

            if (reservation == null)
            {
                return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Reservation.NotFound));
            }

            // Check if we need to re-allocate table
            bool needsReallocation = reservation.ReservationDate != request.ReservationDate ||
                                     reservation.ReservationTime != request.ReservationTime ||
                                     reservation.GuestCount != request.GuestCount ||
                                     reservation.AreaId != request.AreaId;

            if (needsReallocation)
            {
                // Similar logic to CreateInternalReservationHandler
                var query = _unitOfWork.Repository<Table>().Query()
                    .Include(t => t.Area)
                    .Where(t => t.Status != TableStatus.OutOfService && t.Capacity >= request.GuestCount);

                if (request.GuestCount > 8)
                {
                    query = query.Where(t => t.Area.Type == AreaType.VIP);
                }

                if (request.AreaId.HasValue)
                {
                    query = query.Where(t => t.AreaId == request.AreaId.Value);
                }

                var allEligibleTables = await query.ToListAsync(cancellationToken);

                var bufferHours = 2;
                var minTime = request.ReservationTime.Subtract(TimeSpan.FromHours(bufferHours));
                var maxTime = request.ReservationTime.Add(TimeSpan.FromHours(bufferHours));

                var overlappingTableIds = await _unitOfWork.Repository<Reservation>().Query()
                    .Where(r => r.ReservationDate == request.ReservationDate
                                && r.ReservationId != request.ReservationId
                                && r.Status == ReservationStatus.Booked
                                && r.ReservationTime > minTime
                                && r.ReservationTime < maxTime)
                    .Select(r => r.TableId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var availableTable = allEligibleTables
                    .Where(t => !overlappingTableIds.Contains(t.TableId))
                    .OrderBy(t => t.Capacity)
                    .FirstOrDefault();

                if (availableTable == null)
                {
                    if (request.GuestCount > 8)
                    {
                        return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Reservation.VipRequired));
                    }
                    return Result<Guid>.Failure(_messageService.GetMessage(MessageKeys.Reservation.NoTableAvailable));
                }

                reservation.TableId = availableTable.TableId;
                reservation.AreaId = availableTable.AreaId;
            }

            reservation.CustomerName = request.CustomerName;
            reservation.CustomerPhone = request.CustomerPhone;
            reservation.ReservationDate = request.ReservationDate;
            reservation.ReservationTime = request.ReservationTime;
            reservation.GuestCount = request.GuestCount;
            reservation.CustomerName = request.CustomerName;
            reservation.CustomerPhone = request.CustomerPhone;
            reservation.ReservationDate = request.ReservationDate;
            reservation.ReservationTime = request.ReservationTime;
            reservation.GuestCount = request.GuestCount;

            _unitOfWork.Repository<Reservation>().Update(reservation);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully updated Reservation {ReservationId}", reservation.ReservationId);

            return Result<Guid>.Success(reservation.ReservationId);
        }
    }
}
