using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Settings.Common;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Settings.Queries.GetKdsSettings
{
    public class GetKdsSettingsHandler
        : IRequestHandler<GetKdsSettingsQuery, Result<GetKdsSettingsResponse>>
    {
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly ILogger<GetKdsSettingsHandler> _logger;

        public GetKdsSettingsHandler(
            IKdsSettingsProvider kdsSettingsProvider,
            ILogger<GetKdsSettingsHandler> logger
        )
        {
            _kdsSettingsProvider = kdsSettingsProvider;
            _logger = logger;
        }

        public async Task<Result<GetKdsSettingsResponse>> Handle(
            GetKdsSettingsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetKdsSettings");

            var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);

            _logger.LogInformation("End handling GetKdsSettings");
            return Result<GetKdsSettingsResponse>.Success(MapToResponse(settings));
        }

        public static GetKdsSettingsResponse MapToResponse(KdsSettings settings)
        {
            return new GetKdsSettingsResponse
            {
                SortMode = settings.SortMode,
                PriorityWeights = new KdsPriorityWeightsModel
                {
                    WaitTimePerMinute = settings.WaitTimePerMinute,
                    OrderPriorityBonus = settings.OrderPriorityBonus,
                    ExpectedTimeWeight = settings.ExpectedTimeWeight,
                    OverduePerMinute = settings.OverduePerMinute,
                    CompletionBoostWeight = settings.CompletionBoostWeight,
                    TakeawayBonus = settings.TakeawayBonus,
                    DeliveryBonus = settings.DeliveryBonus,
                },
                StationWipLimits = settings.StationWipLimits
                    .OrderBy(x => x.Station)
                    .Select(x => new KdsStationWipLimitModel
                    {
                        Station = x.Station,
                        Limit = x.Limit,
                        Enabled = x.Enabled,
                    })
                    .ToList(),
            };
        }
    }
}
