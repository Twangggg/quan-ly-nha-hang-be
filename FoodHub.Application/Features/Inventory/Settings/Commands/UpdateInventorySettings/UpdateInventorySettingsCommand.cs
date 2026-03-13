using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings
{
    /// <summary>
    /// Updates the inventory behavior and threshold settings used across stock flows.
    /// </summary>
    /// <param name="ExpiryWarningDays">Number of days before expiry when the system starts warning.</param>
    /// <param name="DefaultLowStockThreshold">Default threshold applied when an item is considered low in stock.</param>
    /// <param name="AutoDeductOnCompleted">Indicates whether stock is deducted automatically when an order is completed.</param>
    /// <param name="MaxCostRecalcDays">Maximum lookback window, in days, for cost recalculation.</param>
    public record UpdateInventorySettingsCommand(
        int ExpiryWarningDays,
        decimal DefaultLowStockThreshold,
        bool? AutoDeductOnCompleted,
        int MaxCostRecalcDays
    ) : IRequest<Result<UpdateInventorySettingsResponse>>;
}
