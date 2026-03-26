using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class AuditLog
    {
        public Guid LogId { get; set; }
        public string EntityName { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public AuditAction Action { get; set; } // Create, Update, Delete, StatusChange, ...
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        public string? ActorInfo { get; set; } // JSON or string (Employee ID or Guest Name/Phone)
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
