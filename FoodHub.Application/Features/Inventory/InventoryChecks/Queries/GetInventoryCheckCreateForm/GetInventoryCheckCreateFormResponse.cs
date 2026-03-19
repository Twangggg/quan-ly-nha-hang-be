namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm
{
    public class GetInventoryCheckCreateFormResponse
    {
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal BookQuantity { get; set; }
    }
}
