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
    }
}
