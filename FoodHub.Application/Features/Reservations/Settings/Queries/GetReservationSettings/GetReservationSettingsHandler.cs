using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FoodHub.Application.Features.Reservations.Settings.Queries.GetReservationSettings
{
    public class GetReservationSettingsHandler
        : IRequestHandler<GetReservationSettingsQuery, Result<GetReservationSettingsResponse>>
    {
        private readonly IReservationSettingsProvider _reservationSettingsProvider;
        private readonly ILogger<GetReservationSettingsHandler> _logger;

        public GetReservationSettingsHandler(
            IReservationSettingsProvider reservationSettingsProvider,
            ILogger<GetReservationSettingsHandler> logger
        )
        {
            _reservationSettingsProvider = reservationSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<GetReservationSettingsResponse>> Handle(
            GetReservationSettingsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetReservationSettings");

            var settings = await _reservationSettingsProvider.GetOrCreateAsync(cancellationToken);

            var response = MapToResponse(settings);

            _logger.LogInformation("End handling GetReservationSettings");
            return Result<GetReservationSettingsResponse>.Success(response);
        }

        private static GetReservationSettingsResponse MapToResponse(ReservationSettings settings)
        {
            return new GetReservationSettingsResponse
            {
                OpenTime = FormatTime(settings.OpenTime),
                CloseTime = FormatTime(settings.CloseTime),
                BreakEnabled = settings.BreakEnabled,
                BreakStart = FormatTime(settings.BreakStart),
                BreakEnd = FormatTime(settings.BreakEnd),
                OverlapBufferMinutes = settings.OverlapBufferMinutes,
                MinLeadTimeMinutes = settings.MinLeadTimeMinutes,
                GracePeriodMinutes = settings.GracePeriodMinutes,
            };
        }

        private static string FormatTime(TimeOnly time)
        {
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
