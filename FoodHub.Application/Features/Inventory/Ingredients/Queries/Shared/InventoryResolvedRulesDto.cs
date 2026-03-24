namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.Shared
{
    public sealed class InventoryResolvedRulesDto
    {
        public decimal LowStockThreshold { get; set; }
        public int ExpiryWarningDays { get; set; }
        public InventoryRuleSourceDto Source { get; set; }
    }
}
