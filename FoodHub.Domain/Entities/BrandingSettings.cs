using FoodHub.Domain.Common;

namespace FoodHub.Domain.Entities
{
    public class BrandingSettings : BaseEntity
    {
        public const string DefaultSettingsKey = "branding";
        public const string DefaultRestaurantName = "FoodHub Restaurant";
        public const string DefaultBranchName = "FoodHub";
        public const string DefaultCurrency = "VND";
        public const string DefaultDateFormat = "dd/MM/yyyy";
        public const string DefaultTimezone = "Asia/Ho_Chi_Minh";
        public const string DefaultLanguage = "vi";
        public const string DefaultBillTitle = "HÓA ĐƠN THANH TOÁN";
        public const string DefaultBillFooter = "CẢM ƠN QUÝ KHÁCH - HẸN GẶP LẠI";
        public const string DefaultKdsTitle = "KDS Dashboard";
        public const string DefaultAppTitle = "FoodHub | Premium Restaurant Management";

        protected BrandingSettings() { }

        public Guid BrandingSettingsId { get; private set; }
        public string SettingsKey { get; private set; } = DefaultSettingsKey;
        public string RestaurantName { get; private set; } = DefaultRestaurantName;
        public string BranchName { get; private set; } = DefaultBranchName;
        public string Address { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string Currency { get; private set; } = DefaultCurrency;
        public string DateFormat { get; private set; } = DefaultDateFormat;
        public string Timezone { get; private set; } = DefaultTimezone;
        public string Language { get; private set; } = DefaultLanguage;
        public string BillTitle { get; private set; } = DefaultBillTitle;
        public string BillFooter { get; private set; } = DefaultBillFooter;
        public string KdsTitle { get; private set; } = DefaultKdsTitle;
        public string AppTitle { get; private set; } = DefaultAppTitle;
        public string LogoUrl { get; private set; } = string.Empty;

        // --- Mở rộng thông tin chung ---
        // 1. Business Info
        public string LegalBusinessName { get; private set; } = string.Empty;
        public string BrandName { get; private set; } = string.Empty;
        public string TaxCode { get; private set; } = string.Empty;
        public string BusinessRegistrationNumber { get; private set; } = string.Empty;
        public string BranchCode { get; private set; } = string.Empty;
        public string RestaurantCode { get; private set; } = string.Empty;

        // 2. Contact Info
        public string Hotline { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Website { get; private set; } = string.Empty;
        public string Facebook { get; private set; } = string.Empty;
        public string ZaloOa { get; private set; } = string.Empty;
        public string Instagram { get; private set; } = string.Empty;

        // 3. Address
        public string Country { get; private set; } = string.Empty;
        public string ProvinceCity { get; private set; } = string.Empty;
        public string District { get; private set; } = string.Empty;
        public string Ward { get; private set; } = string.Empty;
        public string StreetAddress { get; private set; } = string.Empty;
        public string PostalCode { get; private set; } = string.Empty;
        public string GoogleMapUrl { get; private set; } = string.Empty;

        // 4. Images
        public string CoverImageUrl { get; private set; } = string.Empty;
        public string QrPaymentImageUrl { get; private set; } = string.Empty;
        public string FaviconUrl { get; private set; } = string.Empty;

        // 5. Invoice Settings
        public decimal VatPercentage { get; private set; } = 0;

        // 6. Time Settings
        public string TimeFormat { get; private set; } = "HH:mm";

        // 7. Operating Info
        public string OpeningTime { get; private set; } = "08:00";
        public string ClosingTime { get; private set; } = "22:00";
        public string WorkingDays { get; private set; } = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday";

        // 8. System Config
        public bool EnableOrdering { get; private set; } = true;
        public bool EnableDelivery { get; private set; } = false;
        public bool EnableTakeAway { get; private set; } = false;
        public bool EnableReservation { get; private set; } = false;

        public static BrandingSettings CreateDefault(Guid? createdBy = null)
        {
            return new BrandingSettings
            {
                BrandingSettingsId = Guid.NewGuid(),
                SettingsKey = DefaultSettingsKey,
                RestaurantName = DefaultRestaurantName,
                BranchName = DefaultBranchName,
                Address = string.Empty,
                Phone = string.Empty,
                Currency = DefaultCurrency,
                DateFormat = DefaultDateFormat,
                Timezone = DefaultTimezone,
                Language = DefaultLanguage,
                BillTitle = DefaultBillTitle,
                BillFooter = DefaultBillFooter,
                KdsTitle = DefaultKdsTitle,
                AppTitle = DefaultAppTitle,
                LogoUrl = string.Empty,
                LegalBusinessName = string.Empty,
                BrandName = string.Empty,
                TaxCode = string.Empty,
                BusinessRegistrationNumber = string.Empty,
                BranchCode = string.Empty,
                RestaurantCode = "REST_001",
                Hotline = string.Empty,
                Email = string.Empty,
                Website = string.Empty,
                Facebook = string.Empty,
                ZaloOa = string.Empty,
                Instagram = string.Empty,
                Country = string.Empty,
                ProvinceCity = string.Empty,
                District = string.Empty,
                Ward = string.Empty,
                StreetAddress = string.Empty,
                PostalCode = string.Empty,
                GoogleMapUrl = string.Empty,
                CoverImageUrl = string.Empty,
                QrPaymentImageUrl = string.Empty,
                FaviconUrl = string.Empty,
                VatPercentage = 0,
                TimeFormat = "HH:mm",
                OpeningTime = "08:00",
                ClosingTime = "22:00",
                WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
                EnableOrdering = true,
                EnableDelivery = false,
                EnableTakeAway = false,
                EnableReservation = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public void Update(
            string restaurantName,
            string branchName,
            string address,
            string phone,
            string currency,
            string dateFormat,
            string timezone,
            string language,
            string billTitle,
            string billFooter,
            string kdsTitle,
            string appTitle,
            string logoUrl,
            string legalBusinessName,
            string brandName,
            string taxCode,
            string businessRegistrationNumber,
            string branchCode,
            string restaurantCode,
            string hotline,
            string email,
            string website,
            string facebook,
            string zaloOa,
            string instagram,
            string country,
            string provinceCity,
            string district,
            string ward,
            string streetAddress,
            string postalCode,
            string googleMapUrl,
            string coverImageUrl,
            string qrPaymentImageUrl,
            string faviconUrl,
            decimal vatPercentage,
            string timeFormat,
            string openingTime,
            string closingTime,
            string workingDays,
            bool enableOrdering,
            bool enableDelivery,
            bool enableTakeAway,
            bool enableReservation,
            Guid? updatedBy = null
        )
        {
            RestaurantName = string.IsNullOrWhiteSpace(restaurantName) ? DefaultRestaurantName : restaurantName.Trim();
            BranchName = string.IsNullOrWhiteSpace(branchName) ? DefaultBranchName : branchName.Trim();
            Address = address?.Trim() ?? string.Empty;
            Phone = phone?.Trim() ?? string.Empty;
            Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim();
            DateFormat = string.IsNullOrWhiteSpace(dateFormat) ? DefaultDateFormat : dateFormat.Trim();
            Timezone = string.IsNullOrWhiteSpace(timezone) ? DefaultTimezone : timezone.Trim();
            Language = string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language.Trim();
            BillTitle = string.IsNullOrWhiteSpace(billTitle) ? DefaultBillTitle : billTitle.Trim();
            BillFooter = string.IsNullOrWhiteSpace(billFooter) ? DefaultBillFooter : billFooter.Trim();
            KdsTitle = string.IsNullOrWhiteSpace(kdsTitle) ? DefaultKdsTitle : kdsTitle.Trim();
            AppTitle = string.IsNullOrWhiteSpace(appTitle) ? DefaultAppTitle : appTitle.Trim();
            LogoUrl = logoUrl?.Trim() ?? string.Empty;
            LegalBusinessName = legalBusinessName?.Trim() ?? string.Empty;
            BrandName = brandName?.Trim() ?? string.Empty;
            TaxCode = taxCode?.Trim() ?? string.Empty;
            BusinessRegistrationNumber = businessRegistrationNumber?.Trim() ?? string.Empty;
            BranchCode = branchCode?.Trim() ?? string.Empty;
            RestaurantCode = restaurantCode?.Trim() ?? string.Empty;
            Hotline = hotline?.Trim() ?? string.Empty;
            Email = email?.Trim() ?? string.Empty;
            Website = website?.Trim() ?? string.Empty;
            Facebook = facebook?.Trim() ?? string.Empty;
            ZaloOa = zaloOa?.Trim() ?? string.Empty;
            Instagram = instagram?.Trim() ?? string.Empty;
            Country = country?.Trim() ?? string.Empty;
            ProvinceCity = provinceCity?.Trim() ?? string.Empty;
            District = district?.Trim() ?? string.Empty;
            Ward = ward?.Trim() ?? string.Empty;
            StreetAddress = streetAddress?.Trim() ?? string.Empty;
            PostalCode = postalCode?.Trim() ?? string.Empty;
            GoogleMapUrl = googleMapUrl?.Trim() ?? string.Empty;
            CoverImageUrl = coverImageUrl?.Trim() ?? string.Empty;
            QrPaymentImageUrl = qrPaymentImageUrl?.Trim() ?? string.Empty;
            FaviconUrl = faviconUrl?.Trim() ?? string.Empty;
            VatPercentage = vatPercentage;
            TimeFormat = string.IsNullOrWhiteSpace(timeFormat) ? "HH:mm" : timeFormat.Trim();
            OpeningTime = string.IsNullOrWhiteSpace(openingTime) ? "08:00" : openingTime.Trim();
            ClosingTime = string.IsNullOrWhiteSpace(closingTime) ? "22:00" : closingTime.Trim();
            WorkingDays = string.IsNullOrWhiteSpace(workingDays) ? "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday" : workingDays.Trim();
            EnableOrdering = enableOrdering;
            EnableDelivery = enableDelivery;
            EnableTakeAway = enableTakeAway;
            EnableReservation = enableReservation;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
