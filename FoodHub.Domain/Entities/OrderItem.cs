using System.Linq;
using System.Text.Json;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
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
        public DateTime? CancelledAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public Order Order { get; set; } = null!;
        public MenuItem MenuItem { get; set; } = null!;
        public ICollection<OrderItemOptionGroup> OptionGroups { get; set; } =
            new List<OrderItemOptionGroup>();

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

        public bool CanCancel() =>
            Status == OrderItemStatus.Preparing
            || Status == OrderItemStatus.Cooking
            || Status == OrderItemStatus.Ready;

        public DomainResult Cancel()
        {
            if (!CanCancel())
            {
                return DomainResult.Failure(DomainErrors.OrderItem.InvalidStatusForCancel);
            }

            Status = OrderItemStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult StartCooking()
        {
            if (Status != OrderItemStatus.Preparing)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.MustBePreparingToStartCooking);
            }
            Status = OrderItemStatus.Cooking;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult MarkReady()
        {
            if (Status != OrderItemStatus.Cooking && Status != OrderItemStatus.Preparing)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.MustBeCookingToReady);
            }
            Status = OrderItemStatus.Ready;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult Reject(string reason)
        {
            if (Status != OrderItemStatus.Cooking && Status != OrderItemStatus.Preparing)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.MustBeCookingToReject);
            }
            if (string.IsNullOrEmpty(reason))
            {
                return DomainResult.Failure(DomainErrors.OrderItem.RejectionReasonIsRequired);
            }
            Status = OrderItemStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
            RejectedAt = DateTime.UtcNow;
            RejectionReason = reason;
            return DomainResult.Success();
        }

        public DomainResult ReturnToQueue()
        {
            if (Status != OrderItemStatus.Rejected)
                return DomainResult.Failure(DomainErrors.OrderItem.MustBeRejectedToReturn);
            Status = OrderItemStatus.Preparing;
            RejectionReason = null;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult UpdateDetails(
            int quantity,
            string? itemNote,
            ICollection<OrderItemOptionGroup> optionGroups
        )
        {
            if (Status != OrderItemStatus.Preparing)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            Quantity = quantity;
            ItemNote = itemNote;
            OptionGroups.Clear();

            foreach (var optionGroup in optionGroups)
            {
                OptionGroups.Add(optionGroup);
            }

            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }
    }
}
