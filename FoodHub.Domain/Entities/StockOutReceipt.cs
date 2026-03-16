using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using static FoodHub.Domain.Constants.DomainErrors;

namespace FoodHub.Domain.Entities
{
    public class StockOutReceipt : BaseEntity
    {
        protected StockOutReceipt() { }
        public Guid StockOutReceiptId { get; private set; }
        public string ReceiptCode { get; private set; } = string.Empty;
        public DateTime StockOutDate { get; private set; }
        public string Note { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public virtual ICollection<StockOutReceiptItem> Items { get; private set; } =
            new List<StockOutReceiptItem>();

        public static StockOutReceipt Create(
            string receiptCode,
            DateTime stockOutDate,
            string? note,
            Guid? createdBy = null
        )
        {
            return new StockOutReceipt
            {
                StockOutReceiptId = Guid.NewGuid(),
                ReceiptCode = receiptCode,
                StockOutDate = stockOutDate,
                Note = string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult AddItem(
            Guid ingredientId,
            decimal quantity,
            decimal? unitPrice,
            Guid? createdBy
        )
        {
            if (Items.Any(x => x.IngredientId == ingredientId))
            {
                return DomainResult.Failure(DomainErrors.StockOutReceipt.DuplicateIngredient);
            }
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.StockOutReceipt.InvalidQuantity);
            }
            if (unitPrice.HasValue && unitPrice.Value < 0)
            {
                return DomainResult.Failure(DomainErrors.StockOutReceipt.InvalidUnitCost);
            }
            var item = StockOutReceiptItem.Create(
                StockOutReceiptId,
                ingredientId,
                quantity,
                unitPrice,
                createdBy
            );
            Items.Add(item);
            RefreshTotals(createdBy);
            return DomainResult.Success();
        }

        public DomainResult Reverse(Guid? updatedBy = null)
        {
            if (DeletedAt.HasValue)
            {
                return DomainResult.Failure(DomainErrors.StockOutReceipt.AlreadyReversed);
            }

            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            foreach (var item in Items)
            {
                item.MarkDeleted(updatedBy);
            }

            return DomainResult.Success();
        }

        private void RefreshTotals(Guid? updatedBy)
        {
            TotalAmount = Items.Sum(x => x.LineAmount);
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
