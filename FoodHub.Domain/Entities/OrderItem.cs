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
        public bool StockDeducted { get; set; }

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

        public bool IsFreeItem { get; set; } // Dùng để đánh dấu món ăn miễn phí được thêm vào bởi voucher loại FreeItem, không phụ thuộc vào giá trị UnitPriceSnapshot

        public ICollection<OrderItemOptionGroup> OptionGroups { get; set; } =
            new List<OrderItemOptionGroup>();

        public decimal GetTotalPrice()
        {
            if (
                Status == OrderItemStatus.Cancelled
                || Status == OrderItemStatus.Rejected
                || IsFreeItem
            )
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

        public bool CanBeMoved() => Status != OrderItemStatus.Completed;

        public bool HasSameConfiguration(OrderItem other)
        {
            if (
                MenuItemId != other.MenuItemId
                || Status != other.Status
                || (ItemNote ?? string.Empty) != (other.ItemNote ?? string.Empty)
                || OptionGroups.Count != other.OptionGroups.Count
            )
            {
                return false;
            }

            foreach (var optionGroup in OptionGroups)
            {
                var matchingGroup = other.OptionGroups.FirstOrDefault(group =>
                    string.Equals(
                        group.GroupNameSnapshot,
                        optionGroup.GroupNameSnapshot,
                        StringComparison.Ordinal
                    )
                    && string.Equals(
                        group.GroupTypeSnapshot,
                        optionGroup.GroupTypeSnapshot,
                        StringComparison.Ordinal
                    )
                    && group.IsRequiredSnapshot == optionGroup.IsRequiredSnapshot
                );

                if (
                    matchingGroup == null
                    || matchingGroup.OptionValues.Count != optionGroup.OptionValues.Count
                )
                {
                    return false;
                }

                foreach (var optionValue in optionGroup.OptionValues)
                {
                    var matchingValue = matchingGroup.OptionValues.FirstOrDefault(value =>
                        value.OptionItemId == optionValue.OptionItemId
                        && value.LabelSnapshot == optionValue.LabelSnapshot
                        && value.ExtraPriceSnapshot == optionValue.ExtraPriceSnapshot
                        && value.Quantity == optionValue.Quantity
                        && (value.Note ?? string.Empty) == (optionValue.Note ?? string.Empty)
                    );

                    if (matchingValue == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void IncreaseQuantity(int quantityToAdd, DateTime updatedAt)
        {
            Quantity += quantityToAdd;
            UpdatedAt = updatedAt;
        }

        public DomainResult ReduceQuantity(int quantityToReduce, DateTime updatedAt)
        {
            if (quantityToReduce <= 0 || quantityToReduce >= Quantity)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.InvalidQuantity);
            }

            Quantity -= quantityToReduce;
            UpdatedAt = updatedAt;
            return DomainResult.Success();
        }

        public DomainResult MoveToOrder(Guid destinationOrderId, DateTime updatedAt)
        {
            if (!CanBeMoved())
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            OrderId = destinationOrderId;
            UpdatedAt = updatedAt;
            return DomainResult.Success();
        }

        public void ReassignToOrder(Guid destinationOrderId, DateTime updatedAt)
        {
            OrderId = destinationOrderId;
            UpdatedAt = updatedAt;
        }

        public OrderItem CloneForOrder(Guid destinationOrderId, int quantity, DateTime createdAt)
        {
            var clonedItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = destinationOrderId,
                MenuItemId = MenuItemId,
                ItemCodeSnapshot = ItemCodeSnapshot,
                ItemNameSnapshot = ItemNameSnapshot,
                StationSnapshot = StationSnapshot,
                Status = Status,
                Quantity = quantity,
                UnitPriceSnapshot = UnitPriceSnapshot,
                ItemNote = ItemNote,
                CreatedAt = createdAt,
            };

            foreach (var optionGroup in OptionGroups)
            {
                var clonedGroup = new OrderItemOptionGroup
                {
                    OrderItemOptionGroupId = Guid.NewGuid(),
                    OrderItemId = clonedItem.OrderItemId,
                    GroupNameSnapshot = optionGroup.GroupNameSnapshot,
                    GroupTypeSnapshot = optionGroup.GroupTypeSnapshot,
                    IsRequiredSnapshot = optionGroup.IsRequiredSnapshot,
                    CreatedAt = createdAt,
                };

                foreach (var optionValue in optionGroup.OptionValues)
                {
                    clonedGroup.OptionValues.Add(
                        new OrderItemOptionValue
                        {
                            OrderItemOptionValueId = Guid.NewGuid(),
                            OrderItemOptionGroupId = clonedGroup.OrderItemOptionGroupId,
                            OptionItemId = optionValue.OptionItemId,
                            LabelSnapshot = optionValue.LabelSnapshot,
                            ExtraPriceSnapshot = optionValue.ExtraPriceSnapshot,
                            Quantity = optionValue.Quantity,
                            Note = optionValue.Note,
                            CreatedAt = createdAt,
                        }
                    );
                }

                clonedItem.OptionGroups.Add(clonedGroup);
            }

            return clonedItem;
        }

        public bool CanCancel() =>
            Status == OrderItemStatus.Preparing || Status == OrderItemStatus.Cooking;

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

        public DomainResult CompleteCooking()
        {
            if (Status != OrderItemStatus.Cooking)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.MustBeCookingToComplete);
            }
            Status = OrderItemStatus.Completed;
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

        public DomainResult AdjustQuantity(int newQuantity)
        {
            if (Status == OrderItemStatus.Cancelled || Status == OrderItemStatus.Rejected)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            if (newQuantity < 1)
            {
                return DomainResult.Failure(DomainErrors.OrderItem.InvalidQuantity);
            }

            Quantity = newQuantity;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }
    }
}
