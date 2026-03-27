using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings
{
    public record UpdateReservationSettingsCommand(
        string OpenTime,
        string CloseTime,
        bool BreakEnabled,
        string BreakStart,
        string BreakEnd,
        int OverlapBufferMinutes,
        int MinLeadTimeMinutes,
        int GracePeriodMinutes,
        int UpcomingBufferMinutes
    ) : IRequest<Result<UpdateReservationSettingsResponse>>;
}
