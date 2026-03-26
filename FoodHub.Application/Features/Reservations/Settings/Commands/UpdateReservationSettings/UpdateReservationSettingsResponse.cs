namespace FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings
{
    public class UpdateReservationSettingsResponse
    {
        public string OpenTime { get; set; } = string.Empty;
        public string CloseTime { get; set; } = string.Empty;
        public bool BreakEnabled { get; set; }
        public string BreakStart { get; set; } = string.Empty;
        public string BreakEnd { get; set; } = string.Empty;
        public int OverlapBufferMinutes { get; set; }
        public int MinLeadTimeMinutes { get; set; }
        public int GracePeriodMinutes { get; set; }
    }
}
