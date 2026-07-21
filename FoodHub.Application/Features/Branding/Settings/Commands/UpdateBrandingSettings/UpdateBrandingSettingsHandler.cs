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
                request.LegalBusinessName,
                request.BrandName,
                request.TaxCode,
                request.BusinessRegistrationNumber,
                request.BranchCode,
                request.RestaurantCode,
                request.Hotline,
                request.Email,
                request.Website,
                request.Facebook,
                request.ZaloOa,
                request.Instagram,
                request.Country,
                request.ProvinceCity,
                request.District,
                request.Ward,
                request.StreetAddress,
                request.PostalCode,
                request.GoogleMapUrl,
                request.CoverImageUrl,
                request.QrPaymentImageUrl,
                request.FaviconUrl,
                request.VatPercentage,
                request.TimeFormat,
                request.OpeningTime,
                request.ClosingTime,
                request.WorkingDays,
                request.EnableOrdering,
                request.EnableDelivery,
                request.EnableTakeAway,
                request.EnableReservation
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
                LegalBusinessName = settings.LegalBusinessName,
                BrandName = settings.BrandName,
                TaxCode = settings.TaxCode,
                BusinessRegistrationNumber = settings.BusinessRegistrationNumber,
                BranchCode = settings.BranchCode,
                RestaurantCode = settings.RestaurantCode,
                Hotline = settings.Hotline,
                Email = settings.Email,
                Website = settings.Website,
                Facebook = settings.Facebook,
                ZaloOa = settings.ZaloOa,
                Instagram = settings.Instagram,
                Country = settings.Country,
                ProvinceCity = settings.ProvinceCity,
                District = settings.District,
                Ward = settings.Ward,
                StreetAddress = settings.StreetAddress,
                PostalCode = settings.PostalCode,
                GoogleMapUrl = settings.GoogleMapUrl,
                CoverImageUrl = settings.CoverImageUrl,
                QrPaymentImageUrl = settings.QrPaymentImageUrl,
                FaviconUrl = settings.FaviconUrl,
                VatPercentage = settings.VatPercentage,
                TimeFormat = settings.TimeFormat,
                OpeningTime = settings.OpeningTime,
                ClosingTime = settings.ClosingTime,
                WorkingDays = settings.WorkingDays,
                EnableOrdering = settings.EnableOrdering,
                EnableDelivery = settings.EnableDelivery,
                EnableTakeAway = settings.EnableTakeAway,
                EnableReservation = settings.EnableReservation
            });
        }
    }
}
