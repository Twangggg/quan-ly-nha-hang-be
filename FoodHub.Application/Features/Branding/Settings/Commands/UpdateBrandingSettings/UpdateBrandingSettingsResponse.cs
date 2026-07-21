namespace FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings
{
    public class UpdateBrandingSettingsResponse
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string DateFormat { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string BillTitle { get; set; } = string.Empty;
        public string BillFooter { get; set; } = string.Empty;
        public string KdsTitle { get; set; } = string.Empty;
        public string AppTitle { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;

        // 1. Business Info
        public string LegalBusinessName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessRegistrationNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string RestaurantCode { get; set; } = string.Empty;

        // 2. Contact Info
        public string Hotline { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
        public string ZaloOa { get; set; } = string.Empty;
        public string Instagram { get; set; } = string.Empty;

        // 3. Address
        public string Country { get; set; } = string.Empty;
        public string ProvinceCity { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string GoogleMapUrl { get; set; } = string.Empty;

        // 4. Images
        public string CoverImageUrl { get; set; } = string.Empty;
        public string QrPaymentImageUrl { get; set; } = string.Empty;
        public string FaviconUrl { get; set; } = string.Empty;

        // 5. Invoice Settings
        public decimal VatPercentage { get; set; }

        // 6. Time Settings
        public string TimeFormat { get; set; } = string.Empty;

        // 7. Operating Info
        public string OpeningTime { get; set; } = string.Empty;
        public string ClosingTime { get; set; } = string.Empty;
        public string WorkingDays { get; set; } = string.Empty;

        // 8. System Config
        public bool EnableOrdering { get; set; }
        public bool EnableDelivery { get; set; }
        public bool EnableTakeAway { get; set; }
        public bool EnableReservation { get; set; }
    }
}
