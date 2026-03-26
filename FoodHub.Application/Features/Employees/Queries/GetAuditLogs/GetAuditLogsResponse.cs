using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Queries.GetAuditLogs
{
    public class GetAuditLogsResponse : IMapFrom<AuditLog>
    {
        public Guid LogId { get; set; }
        public string Action { get; set; } = null!;
        public string? ActorInfo { get; set; }
        public DateTimeOffset Time { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<AuditLog, GetAuditLogsResponse>()
                .ForMember(d => d.Time, opt => opt.MapFrom(s => s.CreatedAt));
        }
    }
}
