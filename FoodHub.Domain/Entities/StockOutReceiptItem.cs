using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class StockOutReceiptItem : BaseEntity
    {
        protected StockOutReceiptItem() { }

        public Guid StockOutReceiptItemId { get; private set; }
        public Guid StockOutReceiptId { get; private set; }
        public Guid IngredientId { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal? UnitPrice { get; private set; }
        public decimal LineAmount { get; private set; }
        public DateTime? CostCalculatedAt { get; private set; }
        public InventoryCostCalculationSource CostCalculationSource { get; private set; }
        public virtual StockOutReceipt StockOutReceipt { get; private set; } = null!;
        public virtual Ingredient Ingredient { get; private set; } = null!;
        public ICollection<StockOutReceiptItemLotAllocation> LotAllocations { get; private set; } =
            new List<StockOutReceiptItemLotAllocation>();

        public static StockOutReceiptItem Create(
            Guid stockOutReceiptId,
            Guid ingredientId,
            decimal quantity,
            decimal? unitPrice,
            Guid? createdBy = null
        )
        {
            var amount = unitPrice.HasValue ? quantity * unitPrice.Value : 0;

            return new StockOutReceiptItem
            {
                StockOutReceiptItemId = Guid.NewGuid(),
                StockOutReceiptId = stockOutReceiptId,
                IngredientId = ingredientId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                LineAmount = amount,
                CostCalculatedAt = unitPrice.HasValue ? DateTime.UtcNow : null,
                CostCalculationSource = InventoryCostCalculationSource.Realtime,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public void RestateCost(
            decimal unitCost,
            InventoryCostCalculationSource calculationSource,
            DateTime calculatedAt,
            Guid? updatedBy = null
        )
        {
            UnitPrice = unitCost;
            LineAmount = Quantity * unitCost;
            CostCalculatedAt = calculatedAt;
            CostCalculationSource = calculationSource;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
