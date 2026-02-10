using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Employees.Queries.GetAuditLogs
{
    public class GetAuditLogsResponse : IMapFrom<AuditLog>
    {
        public Guid LogId { get; set; }
        public string Action { get; set; } = null!;
        public string ActorName { get; set; } = null!;
        public DateTimeOffset Time { get; set; }
        public string? Reason { get; set; }
        public string? Metadata { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<AuditLog, GetAuditLogsResponse>()
                .ForMember(d => d.Action, opt => opt.MapFrom(s => s.Action.ToString()))
                .ForMember(d => d.ActorName, opt => opt.MapFrom(s => s.PerformedBy.FullName))
                .ForMember(d => d.Time, opt => opt.MapFrom(s => s.CreatedAt));
        }
    }
}
