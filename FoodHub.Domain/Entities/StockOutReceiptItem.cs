using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public virtual StockOutReceipt StockOutReceipt { get; private set; } = null!;
        public virtual Ingredient Ingredient { get; private set; } = null!;

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
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }
    }
}
