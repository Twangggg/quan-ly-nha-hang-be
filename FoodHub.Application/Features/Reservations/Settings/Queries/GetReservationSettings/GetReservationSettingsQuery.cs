using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Settings.Queries.GetReservationSettings
{
    public sealed record GetReservationSettingsQuery()
        : IRequest<Result<GetReservationSettingsResponse>>;
}
