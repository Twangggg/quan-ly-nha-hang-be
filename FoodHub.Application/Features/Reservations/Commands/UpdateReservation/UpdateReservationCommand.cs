using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;
using System;

namespace FoodHub.Application.Features.Reservations.Commands.UpdateReservation
{
    public class UpdateReservationCommand : IRequest<Result<Guid>>, IMustBeActive
    {
        public Guid ReservationId { get; set; }
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        public int GuestCount { get; set; }
        public Guid? AreaId { get; set; }
    }
}
