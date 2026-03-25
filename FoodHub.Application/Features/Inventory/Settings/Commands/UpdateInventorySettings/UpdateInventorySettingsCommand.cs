using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings
{
    /// <summary>
    /// Updates the inventory behavior and threshold settings used across stock flows.
    /// </summary>
    /// <param name="ExpiryWarningDays">Number of days before expiry when the system starts warning.</param>
    /// <param name="DefaultLowStockThreshold">Default threshold applied when an item is considered low in stock.</param>
    /// <param name="AutoDeductOnCompleted">Indicates whether stock is deducted automatically when an order is completed.</param>
    /// <param name="CostMethod">Inventory costing method selected by the restaurant.</param>
    /// <param name="MaxCostRecalcDays">Maximum lookback window, in days, for cost recalculation.</param>
    /// <param name="OpeningStockImportCooldownHours">Minimum cooldown, in hours, between two opening-stock imports.</param>
    public record UpdateInventorySettingsCommand(
        int ExpiryWarningDays,
        decimal DefaultLowStockThreshold,
        bool? AutoDeductOnCompleted,
        InventoryCostMethod CostMethod,
        int MaxCostRecalcDays,
        int OpeningStockImportCooldownHours
    ) : IRequest<Result<UpdateInventorySettingsResponse>>;
}
