using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings
{
    public record UpdateBrandingSettingsCommand(
        string RestaurantName,
        string BranchName,
        string Address,
        string Phone,
        string Currency,
        string DateFormat,
        string Timezone,
        string Language,
        string BillTitle,
        string BillFooter,
        string KdsTitle,
        string AppTitle,
        string LogoUrl
    ) : IRequest<Result<UpdateBrandingSettingsResponse>>;
}
