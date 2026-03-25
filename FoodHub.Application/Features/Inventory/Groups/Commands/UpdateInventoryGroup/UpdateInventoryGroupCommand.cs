using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.UpdateInventoryGroup
{
    public sealed record UpdateInventoryGroupCommand(
        Guid InventoryGroupId,
        string Name,
        string? Description = null,
        decimal? LowStockThreshold = null,
        int? ExpiryWarningDays = null,
        InventoryCostMethod? DefaultCostMethod = null
    ) : IRequest<Result<UpdateInventoryGroupResponse>>;
}
