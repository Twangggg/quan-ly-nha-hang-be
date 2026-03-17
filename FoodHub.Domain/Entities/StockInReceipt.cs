using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class StockInReceipt : BaseEntity
    {
        protected StockInReceipt() { }

        public Guid StockInReceiptId { get; private set; }
        public string ReceiptCode { get; private set; } = string.Empty;
        public DateTime ReceivedAt { get; private set; }
        public string? Note { get; private set; }
        public int TotalLines { get; private set; }
        public decimal TotalAmount { get; private set; }
        public virtual ICollection<StockInReceiptItem> Items { get; private set; } =
            new List<StockInReceiptItem>();

        public static StockInReceipt Create(
            string receiptCode,
            DateTime receivedAt,
            string? note,
            Guid? createdBy = null
        )
        {
            return new StockInReceipt
            {
                StockInReceiptId = Guid.NewGuid(),
                ReceiptCode = receiptCode,
                ReceivedAt = receivedAt,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult AddItem(
            Guid ingredientId,
            decimal quantity,
            string baseUnit,
            decimal? unitCost,
            DateTime? expiryDate,
            string? batchCode,
            Guid? createdBy = null
        )
        {
            if (Items.Any(x => x.IngredientId == ingredientId))
            {
                return DomainResult.Failure(DomainErrors.StockInReceipt.DuplicateIngredient);
            }

            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.StockInReceipt.InvalidQuantity);
            }

            if (unitCost.HasValue && unitCost.Value < 0)
            {
                return DomainResult.Failure(DomainErrors.StockInReceipt.InvalidUnitCost);
            }

            var item = StockInReceiptItem.Create(
                StockInReceiptId,
                ingredientId,
                quantity,
                baseUnit,
                unitCost,
                expiryDate,
                batchCode,
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
                return DomainResult.Failure(DomainErrors.StockInReceipt.AlreadyReversed);
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
            TotalLines = Items.Count;
            TotalAmount = Items.Sum(x => x.LineAmount);
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
