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
        public string OperatingDays { get; private set; } = "Thứ 2 - Chủ Nhật";
        public string OperatingHours { get; private set; } = "08:00 - 22:00";
        public string Description { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;

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
                OperatingDays = "Thứ 2 - Chủ Nhật",
                OperatingHours = "08:00 - 22:00",
                Description = string.Empty,
                Email = string.Empty,
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
            string? operatingDays = null,
            string? operatingHours = null,
            string? description = null,
            string? email = null,
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
            OperatingDays = operatingDays?.Trim() ?? "Thứ 2 - Chủ Nhật";
            OperatingHours = operatingHours?.Trim() ?? "08:00 - 22:00";
            Description = description?.Trim() ?? string.Empty;
            Email = email?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
