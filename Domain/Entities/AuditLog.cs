using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class AuditLog
    {
        public Guid LogId { get; set; }
        
        public AuditAction Action { get; set; }
        
        public Guid TargetId { get; set; }
        public virtual Employee Target { get; set; } = null!;
        
        public Guid PerformedByEmployeeId { get; set; }
        public virtual Employee PerformedBy { get; set; } = null!;
        
        public string? Reason { get; set; }
        
        public string? Metadata { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
