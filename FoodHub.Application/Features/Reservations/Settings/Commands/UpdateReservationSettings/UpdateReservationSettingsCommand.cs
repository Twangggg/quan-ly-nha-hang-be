using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings
{
    public record UpdateReservationSettingsCommand
        : IRequest<Result<UpdateReservationSettingsResponse>>
    {
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
        public bool BreakEnabled { get; set; }
        public string BreakStart { get; set; }
        public string BreakEnd { get; set; }
        public int OverlapBufferMinutes { get; set; }
        public int MinLeadTimeMinutes { get; set; }
        public int GracePeriodMinutes { get; set; }
        public int UpcomingBufferMinutes { get; set; }
    }
}
