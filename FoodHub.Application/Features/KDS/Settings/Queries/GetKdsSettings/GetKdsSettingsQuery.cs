using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Settings.Queries.GetKdsSettings
{
    public record GetKdsSettingsQuery() : IRequest<Result<GetKdsSettingsResponse>>;
}
