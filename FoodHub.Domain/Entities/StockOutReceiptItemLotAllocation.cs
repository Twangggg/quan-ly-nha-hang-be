namespace FoodHub.Domain.Entities
{
    public class StockOutReceiptItemLotAllocation : BaseEntity
    {
        protected StockOutReceiptItemLotAllocation() { }

        public Guid StockOutReceiptItemLotAllocationId { get; private set; }
        public Guid StockOutReceiptItemId { get; private set; }
        public Guid InventoryLotId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal UnitCost { get; private set; }
        public decimal LineCost { get; private set; }
        public DateTime OccurredAt { get; private set; }

        public StockOutReceiptItem StockOutReceiptItem { get; private set; } = null!;
        public InventoryLot InventoryLot { get; private set; } = null!;

        public static StockOutReceiptItemLotAllocation Create(
            Guid stockOutReceiptItemId,
            Guid inventoryLotId,
            decimal quantity,
            decimal unitCost,
            DateTime occurredAt,
            Guid? createdBy = null
        )
        {
            return new StockOutReceiptItemLotAllocation
            {
                StockOutReceiptItemLotAllocationId = Guid.NewGuid(),
                StockOutReceiptItemId = stockOutReceiptItemId,
                InventoryLotId = inventoryLotId,
                Quantity = quantity,
                UnitCost = unitCost,
                LineCost = quantity * unitCost,
                OccurredAt = occurredAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public void UpdateCost(decimal unitCost, Guid? updatedBy = null)
        {
            UnitCost = unitCost;
            LineCost = Quantity * unitCost;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
