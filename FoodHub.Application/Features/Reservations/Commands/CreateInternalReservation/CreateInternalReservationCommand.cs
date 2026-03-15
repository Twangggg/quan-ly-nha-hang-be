using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;
using System;

namespace FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation
{
    public class CreateInternalReservationCommand : IRequest<Result<Guid>>
    {
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public int GuestCount { get; set; }
        public string PartyType { get; set; } = string.Empty; // e.g. "normal", "birthday"
        public Guid? AreaId { get; set; }
    }
}
