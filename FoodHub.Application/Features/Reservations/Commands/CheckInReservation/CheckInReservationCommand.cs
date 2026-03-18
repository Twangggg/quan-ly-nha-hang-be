using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Commands.CheckInReservation
{
    public class CheckInReservationCommand : IRequest<Result<CheckInReservationResponse>>, IMustBeActive
    {
        public Guid ReservationId { get; set; }
        public Guid? NewAreaId { get; set; }   
    }
}
