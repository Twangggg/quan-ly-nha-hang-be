using System.Linq;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid OrderId { get; set; }
        public int TransactionCode { get; set; } // Auto-increment for PayOS mapping
        public string OrderCode { get; set; } = null!; // ORD-YYYYMMDD-XXXX
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }

        // Nullable because it is required only for DINE_IN
        public Guid? TableId { get; set; }

        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPriority { get; set; }

        public virtual Employee CreatedByEmployee { get; set; } = null!;
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties
        public virtual Table? Table { get; set; }

        // Billing
        public PaymentMethod? PaymentMethod { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderAuditLog> OrderAuditLogs { get; set; } = new List<OrderAuditLog>();

        public bool CanCancel() => Status == OrderStatus.Serving;

        public DomainResult Cancel()
        {
            if (!CanCancel())
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidStatusForCancel);
            }

            Status = OrderStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;

            if (OrderType == OrderType.DineIn)
            {
                TableId = null;
            }

            foreach (var item in OrderItems)
            {
                item.Cancel();
            }

            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public DomainResult Checkout(Enums.PaymentMethod paymentMethod, decimal? amountReceived)
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidStatusForCheckout);
            }

            if (paymentMethod == Enums.PaymentMethod.Cash)
            {
                if ((amountReceived ?? 0) < TotalAmount)
                {
                    return DomainResult.Failure(DomainErrors.Order.InsufficientAmount);
                }
                AmountPaid = amountReceived;
            }
            else
            {
                // CreditCard, BankTransfer: assume full payment
                AmountPaid = TotalAmount;
            }

            Status = OrderStatus.Paid;
            PaymentMethod = paymentMethod;
            PaidAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        public bool CanComplete() =>
            Status == OrderStatus.Serving && OrderItems.All(oi => oi.IsFinished());

        public DomainResult Complete()
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.OrderNotReadyForCompletion);
            }

            Status = OrderStatus.Completed;
            TotalAmount = OrderItems
                .Where(oi =>
                    oi.Status != OrderItemStatus.Cancelled && oi.Status != OrderItemStatus.Rejected
                )
                .Sum(oi => oi.GetTotalPrice());

            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            if (OrderType == OrderType.DineIn && OrderItems.All(oi => oi.IsFinished()))
            {
                TableId = null;
            }

            return DomainResult.Success();
        }

        public void RecalculateTotalAmount()
        {
            TotalAmount = OrderItems
                .Where(x =>
                    x.Status != OrderItemStatus.Cancelled && x.Status != OrderItemStatus.Rejected
                )
                .Sum(item =>
                {
                    var itemTotal = item.Quantity * item.UnitPriceSnapshot;
                    var optionsTotal =
                        item.OptionGroups?.SelectMany(og => og.OptionValues)
                            .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity)
                        ?? 0;
                    return itemTotal + (optionsTotal * item.Quantity);
                });
        }

        public OrderItem CreateOrderItem(
            MenuItem menuItem,
            int quantity,
            string? note,
            List<(
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            var newItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = this.OrderId,
                MenuItemId = menuItem.MenuItemId,
                Quantity = quantity,
                ItemNote = note,
                CreatedAt = DateTime.UtcNow,
                Status = OrderItemStatus.Preparing,
                ItemNameSnapshot = menuItem.Name,
                ItemCodeSnapshot = menuItem.Code,
                UnitPriceSnapshot = menuItem.GetPriceFor(this.OrderType),
                StationSnapshot = menuItem.Station.ToString(),
            };

            foreach (var optGroup in options)
            {
                var orderItemOptGroup = new OrderItemOptionGroup
                {
                    OrderItemOptionGroupId = Guid.NewGuid(),
                    OrderItemId = newItem.OrderItemId,
                    GroupNameSnapshot = optGroup.Group.Name,
                    GroupTypeSnapshot = optGroup.Group.OptionType.ToString(),
                    IsRequiredSnapshot = optGroup.Group.IsRequired,
                    CreatedAt = DateTime.UtcNow,
                };

                foreach (var selection in optGroup.Selections)
                {
                    var orderItemOptValue = new OrderItemOptionValue
                    {
                        OrderItemOptionValueId = Guid.NewGuid(),
                        OrderItemOptionGroupId = orderItemOptGroup.OrderItemOptionGroupId,
                        OptionItemId = selection.Item.OptionItemId,
                        LabelSnapshot = selection.Item.Label,
                        ExtraPriceSnapshot = selection.Item.ExtraPrice,
                        Quantity = selection.Quantity,
                        Note = selection.Note,
                        CreatedAt = DateTime.UtcNow,
                    };
                    orderItemOptGroup.OptionValues.Add(orderItemOptValue);
                }
                newItem.OptionGroups.Add(orderItemOptGroup);
            }

            return newItem;
        }

        public (OrderItem Item, bool IsNew) AddOrUpdateItem(
            MenuItem menuItem,
            int quantity,
            string? note,
            List<(
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            // Generate signature for matching logic
            var signature = GenerateSignature(options);

            // Try to find existing item to merge
            var existingItem = OrderItems.FirstOrDefault(oi =>
                oi.MenuItemId == menuItem.MenuItemId
                && oi.Status == OrderItemStatus.Preparing
                && (oi.ItemNote ?? string.Empty) == (note ?? string.Empty)
                && GetItemSignature(oi) == signature
            );

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                RecalculateTotalAmount();
                return (existingItem, false);
            }

            // 3. Create new item
            var newItem = CreateOrderItem(menuItem, quantity, note, options);

            OrderItems.Add(newItem);
            RecalculateTotalAmount();
            return (newItem, true);
        }

        public string GenerateSignature(
            List<(
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            if (options == null || !options.Any())
                return string.Empty;

            var allValues = options
                .SelectMany(og => og.Selections)
                .OrderBy(v => v.Item.OptionItemId)
                .Select(v => $"{v.Item.OptionItemId}x{v.Quantity}");

            return string.Join("|", allValues);
        }

        public string GetItemSignature(OrderItem item)
        {
            if (item.OptionGroups == null || !item.OptionGroups.Any())
                return string.Empty;

            var allValues = item
                .OptionGroups.SelectMany(og => og.OptionValues)
                .Where(ov => ov.OptionItemId.HasValue)
                .OrderBy(ov => ov.OptionItemId)
                .Select(ov => $"{ov.OptionItemId}x{ov.Quantity}");

            return string.Join("|", allValues);
        }
    }
}
