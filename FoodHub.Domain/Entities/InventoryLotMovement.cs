using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventoryLotMovement : BaseEntity
    {
        protected InventoryLotMovement() { }

        public Guid InventoryLotMovementId { get; private set; }
        public Guid InventoryLotId { get; private set; }
        public InventoryLotTransactionType TransactionType { get; private set; }
        public decimal QuantityDelta { get; private set; }
        public decimal BalanceAfter { get; private set; }
        public string ReferenceType { get; private set; } = string.Empty;
        public Guid? ReferenceId { get; private set; }
        public string? ReferenceCode { get; private set; }
        public DateTime OccurredAt { get; private set; }
        public decimal? UnitCost { get; private set; }
        public string? Note { get; private set; }

        public InventoryLot InventoryLot { get; private set; } = null!;

        public static InventoryLotMovement Create(
            Guid inventoryLotId,
            InventoryLotTransactionType transactionType,
            decimal quantityDelta,
            decimal balanceAfter,
            string referenceType,
            Guid? referenceId,
            string? referenceCode,
            DateTime occurredAt,
            decimal? unitCost,
            string? note = null,
            Guid? createdBy = null
        )
        {
            return new InventoryLotMovement
            {
                InventoryLotMovementId = Guid.NewGuid(),
                InventoryLotId = inventoryLotId,
                TransactionType = transactionType,
                QuantityDelta = quantityDelta,
                BalanceAfter = balanceAfter,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ReferenceCode = referenceCode,
                OccurredAt = occurredAt,
                UnitCost = unitCost,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }
    }
}
