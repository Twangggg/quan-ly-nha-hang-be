using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Order
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

        // FK to Employee (CreatedBy)
        public Guid CreatedBy { get; set; }
        public virtual Employee CreatedByEmployee { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public Guid? TransactionId { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderAuditLog> OrderAuditLogs { get; set; } = new List<OrderAuditLog>();
    }
}

