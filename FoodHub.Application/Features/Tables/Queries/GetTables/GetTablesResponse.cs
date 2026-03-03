using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    public class GetTablesResponse : IMapFrom<Table>
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public required string Area { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Table, GetTablesResponse>()
                .ForMember(d => d.TableCode, opt => opt.MapFrom(s =>
                (s.Area != null && !string.IsNullOrWhiteSpace(s.Area.CodePrefix)) ? s.Area.CodePrefix + "_" + s.TableNumber : s.TableNumber.ToString()))
                .ForMember(d => d.Area, opt => opt.MapFrom(s => s.Area.Name))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        }
    }
}
