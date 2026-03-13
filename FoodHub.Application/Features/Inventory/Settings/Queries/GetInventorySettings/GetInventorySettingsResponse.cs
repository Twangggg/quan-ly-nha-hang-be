using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings
{
    /// <summary>
    /// Represents the inventory settings returned for read operations.
    /// </summary>
    public class GetInventorySettingsResponse
    {
        /// <summary>
        /// Number of days before expiry when warnings should be shown.
        /// </summary>
        public int ExpiryWarningDays { get; set; }

        /// <summary>
        /// Default low-stock threshold used for inventory items.
        /// </summary>
        public decimal DefaultLowStockThreshold { get; set; }

        /// <summary>
        /// Indicates whether stock is deducted automatically when an order is completed.
        /// </summary>
        public bool AutoDeductOnCompleted { get; set; }

        /// <summary>
        /// Costing method currently used by the inventory module.
        /// </summary>
        public InventoryCostMethod CostMethod { get; set; }

        /// <summary>
        /// Maximum number of days considered when recalculating item costs.
        /// </summary>
        public int MaxCostRecalcDays { get; set; }

        /// <summary>
        /// Current opening-stock workflow status.
        /// </summary>
        public OpeningStockStatus OpeningStockStatus { get; set; }

        /// <summary>
        /// Timestamp when the settings were locked, if applicable.
        /// </summary>
        public DateTime? LockedAt { get; set; }
    }
}
