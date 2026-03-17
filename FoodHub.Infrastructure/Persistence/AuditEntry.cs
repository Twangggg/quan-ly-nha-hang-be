using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace FoodHub.Infrastructure.Persistence
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public string EntityName { get; set; } = null!;
        public string AuditAction { get; set; } = null!;
        public string ActorInfo { get; set; } = null!;
        public Dictionary<string, object?> KeyValues { get; } = new();
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public List<PropertyEntry> TemporaryProperties { get; } = new();

        public bool HasChanges => OldValues.Any() || NewValues.Any();

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                LogId = Guid.NewGuid(),
                EntityName = EntityName,
                EntityId = JsonSerializer.Serialize(KeyValues),
                Action = AuditAction,
                OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
                NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
                ActorInfo = ActorInfo,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
