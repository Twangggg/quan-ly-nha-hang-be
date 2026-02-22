using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;
using static FoodHub.Domain.Constants.DomainErrors;

namespace FoodHub.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid OrderId { get; set; }
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

        public Guid? TransactionId { get; set; }
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

        public bool CanComplete() =>
            Status == OrderStatus.Serving && OrderItems.All(oi => oi.IsFinished());

        public DomainResult Complete()
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidStatusForCancel);
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
            TotalAmount = OrderItems.Where(x =>
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
    }
}
