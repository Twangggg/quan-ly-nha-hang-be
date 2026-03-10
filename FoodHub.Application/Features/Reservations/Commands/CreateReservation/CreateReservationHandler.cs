using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateReservationHandler> _logger;

        public CreateReservationHandler(IUnitOfWork unitOfWork, ILogger<CreateReservationHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra bàn tồn tại và ACTIVE (DeletedAt == null already handled by global query filter)
            var table = await _unitOfWork.Repository<Table>()
                .Query()
                .FirstOrDefaultAsync(t => t.TableId == request.TableId, cancellationToken);

            if (table == null)
            {
                return Result<Guid>.Failure("Bàn không tồn tại hoặc đã ngưng hoạt động.", ResultErrorType.NotFound);
            }

            // Kiểm tra sức chứa
            if (table.Capacity < request.GuestCount)
            {
                return Result<Guid>.Failure("Bàn không đủ sức chứa cho số lượng khách.", ResultErrorType.BadRequest);
            }

            // Check overlapping (cùng ngày, cách nhau dưới 2h, status = Booked)
            var bufferHours = 2;
            var minTime = request.ReservationTime.Subtract(TimeSpan.FromHours(bufferHours));
            var maxTime = request.ReservationTime.Add(TimeSpan.FromHours(bufferHours));

            var isOverlapped = await _unitOfWork.Repository<Reservation>().Query()
                .AnyAsync(r => r.TableId == request.TableId 
                               && r.ReservationDate == request.ReservationDate 
                               && r.Status == ReservationStatus.Booked
                               && r.ReservationTime > minTime 
                               && r.ReservationTime < maxTime, 
                          cancellationToken);

            if (isOverlapped)
            {
                return Result<Guid>.Failure("Bàn đã được đặt trong khoảng thời gian này.", ResultErrorType.Conflict);
            }

            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                ReservationDate = request.ReservationDate,
                ReservationTime = request.ReservationTime,
                PartyType = request.PartyType,
                GuestCount = request.GuestCount,
                HasChildren = request.HasChildren,
                Note = request.Note,
                Status = ReservationStatus.Booked,
                TableId = request.TableId
            };

            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully created Reservation ID {ReservationId} for Table {TableId}", reservation.ReservationId, request.TableId);

            return Result<Guid>.Success(reservation.ReservationId);
        }
    }
}
