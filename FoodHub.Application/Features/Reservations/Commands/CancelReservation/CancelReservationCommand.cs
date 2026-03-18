using FoodHub.Application.Common.Models;
using MediatR;
using System;

namespace FoodHub.Application.Features.Reservations.Commands.CancelReservation
{
    public record CancelReservationCommand(Guid ReservationId) : IRequest<Result<string>>;
}
