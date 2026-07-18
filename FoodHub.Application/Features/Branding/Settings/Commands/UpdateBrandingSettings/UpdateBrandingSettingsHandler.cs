using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Branding;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings
{
    public class UpdateBrandingSettingsHandler
        : IRequestHandler<UpdateBrandingSettingsCommand, Result<UpdateBrandingSettingsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;
        private readonly ILogger<UpdateBrandingSettingsHandler> _logger;

        public UpdateBrandingSettingsHandler(
            IUnitOfWork unitOfWork,
            IBrandingSettingsProvider brandingSettingsProvider,
            ILogger<UpdateBrandingSettingsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _brandingSettingsProvider = brandingSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<UpdateBrandingSettingsResponse>> Handle(
            UpdateBrandingSettingsCommand request,
            CancellationToken cancellationToken
        )
        {
            var settings = await _brandingSettingsProvider.GetOrCreateAsync(cancellationToken);
            settings.Update(
                request.RestaurantName,
                request.BranchName,
                request.Address,
                request.Phone,
                request.Currency,
                request.DateFormat,
                request.Timezone,
                request.Language,
                request.BillTitle,
                request.BillFooter,
                request.KdsTitle,
                request.AppTitle,
                request.LogoUrl,
                request.OperatingDays,
                request.OperatingHours,
                request.Description,
                request.Email
            );

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            _logger.LogInformation("Handled UpdateBrandingSettings");
            return Result<UpdateBrandingSettingsResponse>.Success(new UpdateBrandingSettingsResponse
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
                OperatingDays = settings.OperatingDays,
                OperatingHours = settings.OperatingHours,
                Description = settings.Description,
                Email = settings.Email,
            });
        }
    }
}
