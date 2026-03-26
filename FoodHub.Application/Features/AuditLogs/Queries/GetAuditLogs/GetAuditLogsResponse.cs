using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsResponse : IMapFrom<AuditLog>
    {
        public Guid LogId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string? Summary { get; set; }
        public string? ActorInfo { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
