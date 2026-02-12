using System.Linq;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OrderItem
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }

        // Snapshots
        public string ItemCodeSnapshot { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;

        public OrderItemStatus Status { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }

        public string? ItemNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CanceledAt { get; set; }

        public decimal GetTotalPrice()
        {
            if (Status == OrderItemStatus.Cancelled || Status == OrderItemStatus.Rejected)
                return 0;

            var optionsTotal =
                OptionGroups
                    ?.SelectMany(og => og.OptionValues)
                    .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity)
                ?? 0;

            return Quantity * (UnitPriceSnapshot + optionsTotal);
        }

        public bool IsFinished() =>
            Status == OrderItemStatus.Completed
            || Status == OrderItemStatus.Cancelled
            || Status == OrderItemStatus.Rejected;

        public DomainResult Cancel()
        {
            if (
                Status == OrderItemStatus.Preparing
                || Status == OrderItemStatus.Cooking
                || Status == OrderItemStatus.Ready
            )
            {
                Status = OrderItemStatus.Cancelled;
                CanceledAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
            }
            return DomainResult.Success();
        }

        public Order Order { get; set; } = null!;
        public ICollection<OrderItemOptionGroup> OptionGroups { get; set; } =
            new List<OrderItemOptionGroup>();
    }
}
