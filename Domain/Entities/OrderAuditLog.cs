namespace FoodHub.Domain.Entities
{
    public class OrderAuditLog
    {
        public Guid LogId { get; set; }
        public Guid OrderId { get; set; }
        public Guid EmployeeId { get; set; }

        public string Action { get; set; } = null!; // CREATE, ADD_ITEM, UPDATE_QTY, SUBMIT, CANCEL

        public string? OldValue { get; set; } // JSONB
        public string? NewValue { get; set; } // JSONB
        public string? ChangeReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public virtual Order Order { get; set; } = null!;
        public virtual Employee Employee { get; set; } = null!;
    }
}
