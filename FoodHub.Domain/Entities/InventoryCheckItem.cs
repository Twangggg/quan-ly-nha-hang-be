namespace FoodHub.Domain.Entities
{
    public class InventoryCheckItem : BaseEntity
    {
        protected InventoryCheckItem() { }

        public Guid InventoryCheckItemId { get; private set; }
        public Guid InventoryCheckId { get; private set; }
        public Guid IngredientId { get; private set; }
        public decimal BookQuantity { get; private set; }
        public decimal PhysicalQuantity { get; private set; }
        public decimal DifferenceQuantity { get; private set; }
        public string? Reason { get; private set; }

        public virtual InventoryCheck InventoryCheck { get; private set; } = null!;
        public virtual Ingredient Ingredient { get; private set; } = null!;

        public static InventoryCheckItem Create(
            Guid inventoryCheckId,
            Guid ingredientId,
            decimal bookQuantity,
            decimal physicalQuantity,
            string? reason,
            Guid? createdBy = null
        )
        {
            return new InventoryCheckItem
            {
                InventoryCheckItemId = Guid.NewGuid(),
                InventoryCheckId = inventoryCheckId,
                IngredientId = ingredientId,
                BookQuantity = bookQuantity,
                PhysicalQuantity = physicalQuantity,
                DifferenceQuantity = physicalQuantity - bookQuantity,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }
    }
}
