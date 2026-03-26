using FoodHub.Application.Features.KDS.Settings.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.KDS.Settings.Commands.UpdateKdsSettings
{
    public class UpdateKdsSettingsResponse
    {
        public KdsSortMode SortMode { get; set; }
        public KdsPriorityWeightsModel PriorityWeights { get; set; } = new();
        public List<KdsStationWipLimitModel> StationWipLimits { get; set; } = [];
    }
}
