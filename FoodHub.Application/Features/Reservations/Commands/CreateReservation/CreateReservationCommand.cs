using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationCommand : IRequest<Result<Guid>>
    {
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public PartyType PartyType { get; set; }
        public int GuestCount { get; set; }
        public bool HasChildren { get; set; }
        public string? Note { get; set; }
        public Guid TableId { get; set; }
    }
}
