using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Branding.Settings.Queries.GetBrandingSettings
{
    public record GetBrandingSettingsQuery() : IRequest<Result<GetBrandingSettingsResponse>>;
}
