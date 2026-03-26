using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Settings.Common;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.KDS.Settings.Commands.UpdateKdsSettings
{
    public record UpdateKdsSettingsCommand(
        KdsSortMode SortMode,
        KdsPriorityWeightsModel PriorityWeights,
        List<KdsStationWipLimitModel> StationWipLimits
    ) : IRequest<Result<UpdateKdsSettingsResponse>>;
}
