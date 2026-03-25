using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Promotion : BaseEntity
    {
        public Guid PromotionId { get; set; }
        public string Code { get; set; } = default!;
        public PromotionType Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public Guid? ItemId { get; set; }
        public virtual MenuItem? Item { get; set; }
        public int? FreeQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public DomainResult Validate(decimal subTotal, DateTimeOffset currentTime)
        {
            if (!IsActive)
                return DomainResult.Failure(DomainErrors.Promotion.Inactive);

            var currentDate = currentTime.UtcDateTime;

            if (currentDate < StartDate)
                return DomainResult.Failure(DomainErrors.Promotion.NotStarted);

            if (currentDate > EndDate)
                return DomainResult.Failure(DomainErrors.Promotion.Expired);

            if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value)
                return DomainResult.Failure(DomainErrors.Promotion.UsageLimitExceeded);

            if (StartTime.HasValue && EndTime.HasValue)
            {
                var timeOfDay = currentTime.TimeOfDay;
                if (timeOfDay < StartTime.Value || timeOfDay > EndTime.Value)
                    return DomainResult.Failure(DomainErrors.Promotion.InvalidTime);
            }

            if (MinOrderValue.HasValue && subTotal < MinOrderValue.Value)
                return DomainResult.Failure(DomainErrors.Promotion.BelowMinAmount);

            return DomainResult.Success();
        }

        public bool IsValid() => Validate(0, DateTimeOffset.UtcNow).IsSuccess;

        public bool IsBelowMinAmount(decimal subTotal) =>
            MinOrderValue.HasValue && subTotal < MinOrderValue.Value;

        public void Used(Guid auditorId) => IncrementUsed(auditorId);

        public void UnUsed(Guid auditorId) => DecrementUsed(auditorId);

        public bool IsFreeItemInOrder(Order order)
        {
            if (Type != PromotionType.FreeItem || !ItemId.HasValue)
                return false;

            return order.OrderItems.Any(oi => oi.MenuItemId == ItemId.Value && !oi.IsFreeItem);
        }

        public void IncrementUsed(Guid auditorId)
        {
            UsedCount++;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;
        }

        public void DecrementUsed(Guid auditorId)
        {
            if (UsedCount > 0)
                UsedCount--;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;
        }
    }
}
