using System.Linq;
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
            _logger.LogInformation(
                "Creating reservation request for Table {TableId} on {ReservationDate} at {ReservationTime} for {GuestCount} guests",
                request.TableId,
                request.ReservationDate,
                request.ReservationTime,
                request.GuestCount
            );

            var table = await _unitOfWork.Repository<Table>()
                .Query()
                .FirstOrDefaultAsync(
                    t => t.TableId == request.TableId && t.Status != TableStatus.OutOfService,
                    cancellationToken
                );

            if (table == null)
            {
                return Result<Guid>.Failure(
                    "Ban khong ton tai hoac da ngung hoat dong.",
                    ResultErrorType.NotFound
                );
            }

            var reservation = Reservation.CreateBooked(
                request.CustomerName,
                request.CustomerPhone,
                request.ReservationDate,
                request.ReservationTime,
                request.PartyType,
                request.GuestCount,
                request.HasChildren,
                request.Note,
                request.TableId,
                table.AreaId
            );

            if (!reservation.CanFitTable(table))
            {
                return Result<Guid>.Failure(
                    "Ban khong du suc chua cho so luong khach.",
                    ResultErrorType.BadRequest
                );
            }

            var existingReservations = await _unitOfWork.Repository<Reservation>()
                .Query()
                .Where(r => r.TableId == request.TableId && r.ReservationDate == request.ReservationDate)
                .ToListAsync(cancellationToken);

            if (existingReservations.Any(existingReservation => reservation.OverlapsWith(existingReservation)))
            {
                return Result<Guid>.Failure(
                    "Ban da duoc dat trong khoang thoi gian nay.",
                    ResultErrorType.Conflict
                );
            }

            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created Reservation ID {ReservationId} for Table {TableId}",
                reservation.ReservationId,
                request.TableId
            );

            return Result<Guid>.Success(reservation.ReservationId);
        }
    }
}
