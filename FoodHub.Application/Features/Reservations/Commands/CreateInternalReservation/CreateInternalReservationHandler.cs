using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation
{
    public class CreateInternalReservationHandler : IRequestHandler<CreateInternalReservationCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateInternalReservationHandler> _logger;
        private readonly IMessageService _messageService;

        public CreateInternalReservationHandler(IUnitOfWork unitOfWork, ILogger<CreateInternalReservationHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<Guid>> Handle(CreateInternalReservationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start creating internal reservation for {CustomerName}", request.CustomerName);

            // Tìm bàn còn trống dựa theo AreaId và GuestCount
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
                            && r.Status == ReservationStatus.Booked
                            && r.ReservationTime > minTime
                            && r.ReservationTime < maxTime)
                .Select(r => r.TableId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var availableTable = allEligibleTables
                .Where(t => !overlappingTableIds.Contains(t.TableId))
                .OrderBy(t => t.Capacity) // Prioritize smaller tables that can fit the guests
                .FirstOrDefault();

            if (availableTable == null)
            {
                if (request.GuestCount > 8)
                {
                    _logger.LogWarning("No VIP table available for group of {GuestCount}", request.GuestCount);
                    throw new BusinessException(_messageService.GetMessage(MessageKeys.Reservation.VipRequired));
                }

                _logger.LogWarning("No table available for {GuestCount} guests at requested time.", request.GuestCount);
                throw new BusinessException(_messageService.GetMessage(MessageKeys.Reservation.NoTableAvailable));
            }

            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                ReservationDate = request.ReservationDate,
                ReservationTime = request.ReservationTime,
                GuestCount = request.GuestCount,
                Note = "Created by Internal User",
                Status = ReservationStatus.Booked,
                AreaId = availableTable.AreaId,
                TableId = availableTable.TableId
            };

            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully created Reservation {ReservationId} at Table {TableId}", reservation.ReservationId, reservation.TableId);

            return Result<Guid>.Success(reservation.ReservationId);
        }
    }
}
