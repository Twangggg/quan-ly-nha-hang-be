namespace FoodHub.Domain.Entities
{
    public class StockInReceiptItem : BaseEntity
    {
        protected StockInReceiptItem() { }

        public Guid StockInReceiptItemId { get; private set; }
        public Guid StockInReceiptId { get; private set; }
        public Guid IngredientId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal? UnitCost { get; private set; }
        public string BaseUnit { get; private set; } = string.Empty;
        public decimal LineAmount { get; private set; }
        public DateTime? ExpiryDate { get; private set; }
        public string? BatchCode { get; private set; }

        public StockInReceipt StockInReceipt { get; private set; } = null!;
        public Ingredient Ingredient { get; private set; } = null!;
        public ICollection<InventoryLot> InventoryLots { get; private set; } = new List<InventoryLot>();

        public static StockInReceiptItem Create(
            Guid stockInReceiptId,
            Guid ingredientId,
            decimal quantity,
            string baseUnit,
            decimal? unitCost,
            DateTime? expiryDate,
            string? batchCode,
            Guid? createdBy = null
        )
        {
            var amount = unitCost.HasValue ? quantity * unitCost.Value : 0;

            return new StockInReceiptItem
            {
                StockInReceiptItemId = Guid.NewGuid(),
                StockInReceiptId = stockInReceiptId,
                IngredientId = ingredientId,
                Quantity = quantity,
                BaseUnit = baseUnit,
                UnitCost = unitCost,
                LineAmount = amount,
                ExpiryDate = expiryDate,
                BatchCode = string.IsNullOrWhiteSpace(batchCode) ? null : batchCode.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }
    }
}
