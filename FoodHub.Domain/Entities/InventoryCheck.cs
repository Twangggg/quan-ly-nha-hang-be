using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventoryCheck : BaseEntity
    {
        protected InventoryCheck() { }

        public Guid InventoryCheckId { get; private set; }
        public DateTime CheckDate { get; private set; }
        public InventoryCheckStatus Status { get; private set; }
        public DateTime? ProcessedAt { get; private set; }
        public virtual ICollection<InventoryCheckItem> Items { get; private set; } =
            new List<InventoryCheckItem>();

        public static InventoryCheck Create(DateTime checkDate, Guid? createdBy = null)
        {
            return new InventoryCheck
            {
                InventoryCheckId = Guid.NewGuid(),
                CheckDate = checkDate,
                Status = InventoryCheckStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult AddItem(
            Guid ingredientId,
            decimal bookQuantity,
            decimal physicalQuantity,
            string? reason,
            Guid? createdBy = null
        )
        {
            if (Items.Any(x => x.IngredientId == ingredientId))
            {
                return DomainResult.Failure(DomainErrors.InventoryCheck.DuplicateIngredient);
            }

            if (bookQuantity < 0 || physicalQuantity < 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryCheck.InvalidQuantity);
            }

            Items.Add(
                InventoryCheckItem.Create(
                    InventoryCheckId,
                    ingredientId,
                    bookQuantity,
                    physicalQuantity,
                    reason,
                    createdBy
                )
            );

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = createdBy;

            return DomainResult.Success();
        }

        public DomainResult EnsureProcessable()
        {
            if (Status != InventoryCheckStatus.Draft)
            {
                return DomainResult.Failure(DomainErrors.InventoryCheck.InvalidStatus);
            }

            if (Items.Count == 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryCheck.ItemsRequired);
            }

            return DomainResult.Success();
        }

        public DomainResult MarkProcessed(Guid? updatedBy = null)
        {
            var processableResult = EnsureProcessable();
            if (!processableResult.IsSuccess)
            {
                return processableResult;
            }

            Status = InventoryCheckStatus.Processed;
            ProcessedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }
    }
}
