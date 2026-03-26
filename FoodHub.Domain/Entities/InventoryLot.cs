using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventoryLot : BaseEntity
    {
        private const int LotCodeMaxLength = 100;
        private const int NotesMaxLength = 500;

        protected InventoryLot() { }

        public Guid InventoryLotId { get; private set; }
        public Guid IngredientId { get; private set; }
        public Guid? StockInReceiptItemId { get; private set; }
        public string LotCode { get; private set; } = string.Empty;
        public DateTime ReceivedAt { get; private set; }
        public DateTime? ExpiryDate { get; private set; }
        public decimal UnitCost { get; private set; }
        public decimal OriginalQuantity { get; private set; }
        public decimal RemainingQuantity { get; private set; }
        public decimal ReservedQuantity { get; private set; }
        public InventoryLotStatus Status { get; private set; }
        public string? Notes { get; private set; }

        public Ingredient Ingredient { get; private set; } = null!;
        public StockInReceiptItem? StockInReceiptItem { get; private set; }
        public ICollection<InventoryLotMovement> Movements { get; private set; } =
            new List<InventoryLotMovement>();

        public static DomainResult<InventoryLot> Create(
            Guid ingredientId,
            Guid? stockInReceiptItemId,
            string lotCode,
            DateTime receivedAt,
            DateTime? expiryDate,
            decimal unitCost,
            decimal quantity,
            string? notes = null,
            Guid? createdBy = null
        )
        {
            if (quantity <= 0)
            {
                return DomainResult<InventoryLot>.Failure(
                    DomainErrors.InventoryLot.InvalidQuantity
                );
            }

            if (unitCost < 0)
            {
                return DomainResult<InventoryLot>.Failure(
                    DomainErrors.InventoryLot.InvalidUnitCost
                );
            }

            if (string.IsNullOrWhiteSpace(lotCode))
            {
                return DomainResult<InventoryLot>.Failure(
                    DomainErrors.InventoryLot.LotCodeRequired
                );
            }

            var normalizedLotCode = lotCode.Trim();
            if (normalizedLotCode.Length > LotCodeMaxLength)
            {
                return DomainResult<InventoryLot>.Failure(DomainErrors.InventoryLot.LotCodeTooLong);
            }

            var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (normalizedNotes?.Length > NotesMaxLength)
            {
                return DomainResult<InventoryLot>.Failure(DomainErrors.InventoryLot.NotesTooLong);
            }

            var lot = new InventoryLot
            {
                InventoryLotId = Guid.NewGuid(),
                IngredientId = ingredientId,
                StockInReceiptItemId = stockInReceiptItemId,
                LotCode = normalizedLotCode,
                ReceivedAt = receivedAt,
                ExpiryDate = expiryDate,
                UnitCost = unitCost,
                OriginalQuantity = quantity,
                RemainingQuantity = quantity,
                ReservedQuantity = 0,
                Notes = normalizedNotes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };

            lot.RefreshStatus(receivedAt.Date);
            return DomainResult<InventoryLot>.Success(lot);
        }

        public DomainResult Consume(decimal quantity, DateTime occurredAt, Guid? updatedBy = null)
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InvalidQuantity);
            }

            if (!CanConsume(occurredAt))
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.Expired);
            }

            if (GetAvailableQuantity(occurredAt) < quantity)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InsufficientQuantity);
            }

            RemainingQuantity -= quantity;
            Touch(updatedBy);
            RefreshStatus(occurredAt.Date);

            return DomainResult.Success();
        }

        public DomainResult ReverseConsume(
            decimal quantity,
            DateTime occurredAt,
            Guid? updatedBy = null
        )
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InvalidQuantity);
            }

            if (RemainingQuantity + quantity > OriginalQuantity)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InvalidAdjustment);
            }

            RemainingQuantity += quantity;
            Touch(updatedBy);
            RefreshStatus(occurredAt.Date);

            return DomainResult.Success();
        }

        public DomainResult MarkDisposed(
            decimal quantity,
            string reason,
            DateTime occurredAt,
            Guid? updatedBy = null
        )
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InvalidQuantity);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.ReasonRequired);
            }

            if (Status == InventoryLotStatus.Disposed)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.AlreadyDisposed);
            }

            if (RemainingQuantity < quantity)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InsufficientQuantity);
            }

            RemainingQuantity -= quantity;
            Notes = reason.Trim();
            Touch(updatedBy);
            Status = RemainingQuantity == 0 ? InventoryLotStatus.Disposed : Status;
            RefreshStatus(occurredAt.Date);

            return DomainResult.Success();
        }

        public DomainResult AdjustQuantity(
            decimal delta,
            string reason,
            DateTime occurredAt,
            Guid? updatedBy = null
        )
        {
            if (delta == 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InvalidAdjustment);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.ReasonRequired);
            }

            if (RemainingQuantity + delta < 0)
            {
                return DomainResult.Failure(DomainErrors.InventoryLot.InsufficientQuantity);
            }

            RemainingQuantity += delta;
            OriginalQuantity = Math.Max(OriginalQuantity, RemainingQuantity);
            Notes = reason.Trim();
            Touch(updatedBy);
            RefreshStatus(occurredAt.Date);

            return DomainResult.Success();
        }

        public void MarkExpired(DateTime currentDate, Guid? updatedBy = null)
        {
            if (
                ExpiryDate.HasValue
                && ExpiryDate.Value.Date < currentDate.Date
                && RemainingQuantity > 0
            )
            {
                Status = InventoryLotStatus.Expired;
                Touch(updatedBy);
            }
        }

        public bool CanConsume(DateTime onDate)
        {
            if (DeletedAt.HasValue || Status == InventoryLotStatus.Disposed)
            {
                return false;
            }

            if (RemainingQuantity <= 0)
            {
                return false;
            }

            return !ExpiryDate.HasValue || ExpiryDate.Value.Date >= onDate.Date;
        }

        public decimal GetAvailableQuantity(DateTime onDate)
        {
            if (!CanConsume(onDate))
            {
                return 0;
            }

            var available = RemainingQuantity - ReservedQuantity;
            return available < 0 ? 0 : available;
        }

        public bool CanReverseSourceStockIn()
        {
            return !DeletedAt.HasValue
                && RemainingQuantity == OriginalQuantity
                && Status != InventoryLotStatus.Disposed;
        }

        public void RefreshStatus(DateTime currentDate, int expiryWarningDays = 7)
        {
            if (DeletedAt.HasValue)
            {
                return;
            }

            if (Status == InventoryLotStatus.Disposed && RemainingQuantity == 0)
            {
                return;
            }

            if (RemainingQuantity <= 0)
            {
                Status = InventoryLotStatus.Depleted;
                return;
            }

            if (!ExpiryDate.HasValue)
            {
                Status = InventoryLotStatus.Active;
                return;
            }

            if (ExpiryDate.Value.Date < currentDate.Date)
            {
                Status = InventoryLotStatus.Expired;
                return;
            }

            if (ExpiryDate.Value.Date <= currentDate.Date.AddDays(expiryWarningDays))
            {
                Status = InventoryLotStatus.NearExpiry;
                return;
            }

            Status = InventoryLotStatus.Active;
        }

        private void Touch(Guid? updatedBy)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }
    }
}
