namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck
{
    public class CreateInventoryCheckItemDto
    {
        public Guid IngredientId { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public string? Reason { get; set; }
    }
}
