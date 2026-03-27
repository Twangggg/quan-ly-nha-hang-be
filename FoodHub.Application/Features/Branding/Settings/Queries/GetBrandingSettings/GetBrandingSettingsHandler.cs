using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Branding.Settings.Queries.GetBrandingSettings
{
    public class GetBrandingSettingsHandler
        : IRequestHandler<GetBrandingSettingsQuery, Result<GetBrandingSettingsResponse>>
    {
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;
        private readonly ILogger<GetBrandingSettingsHandler> _logger;

        public GetBrandingSettingsHandler(
            IBrandingSettingsProvider brandingSettingsProvider,
            ILogger<GetBrandingSettingsHandler> logger
        )
        {
            _brandingSettingsProvider = brandingSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<GetBrandingSettingsResponse>> Handle(
            GetBrandingSettingsQuery request,
            CancellationToken cancellationToken
        )
        {
            var settings = await _brandingSettingsProvider.GetOrCreateAsync(cancellationToken);
            _logger.LogInformation("Handled GetBrandingSettings");
            return Result<GetBrandingSettingsResponse>.Success(MapToResponse(settings));
        }

        public static GetBrandingSettingsResponse MapToResponse(BrandingSettings settings)
        {
            return new GetBrandingSettingsResponse
            {
                RestaurantName = settings.RestaurantName,
                BranchName = settings.BranchName,
                Address = settings.Address,
                Phone = settings.Phone,
                Currency = settings.Currency,
                DateFormat = settings.DateFormat,
                Timezone = settings.Timezone,
                Language = settings.Language,
                BillTitle = settings.BillTitle,
                BillFooter = settings.BillFooter,
                KdsTitle = settings.KdsTitle,
                AppTitle = settings.AppTitle,
                LogoUrl = settings.LogoUrl,
            };
        }
    }
}
