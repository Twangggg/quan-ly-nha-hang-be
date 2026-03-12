using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommand : IRequest<Result<CreateReservationResponse>>
    {
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public int GuestCount { get; set; }
        public string? Note { get; set; }
        public Guid AreaId { get; set; }
    }

    public class CreateReservationResponse
    {
        public Guid ReservationId { get; set; }
        public Guid TableId { get; set; }
    }
}
