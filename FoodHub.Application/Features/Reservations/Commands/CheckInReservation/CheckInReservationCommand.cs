using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Commands.CheckInReservation
{
    public class CheckInReservationCommand : IRequest<Result<CheckInReservationResponse>>
    {
        public Guid ReservationId { get; set; }
    }
}
