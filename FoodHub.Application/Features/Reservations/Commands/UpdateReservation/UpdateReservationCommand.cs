using FoodHub.Application.Common.Models;
using MediatR;
using System;

namespace FoodHub.Application.Features.Reservations.Commands.UpdateReservation
{
    public class UpdateReservationCommand : IRequest<Result<Guid>>
    {
        public Guid ReservationId { get; set; }
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public int GuestCount { get; set; }
        public string PartyType { get; set; } = string.Empty;
        public Guid? AreaId { get; set; }
    }
}
