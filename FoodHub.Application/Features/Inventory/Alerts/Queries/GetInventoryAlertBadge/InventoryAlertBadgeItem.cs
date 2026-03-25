namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlertBadge
{
    public sealed class InventoryAlertBadgeItem
    {
        public Guid IngredientId { get; set; }
        public InventoryRuleSourceDto Source { get; set; }
    }
}
