using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Features.Tables.Queries.GetTables;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Queries.GetTableById
{
    public class GetTableByIdResponse : IMapFrom<Table>
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public Guid AreaId { get; set; } = Guid.Empty;
        public required string AreaName { get; set; }
        public required int Status { get; set; }
        public required string StatusName { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Table, GetTableByIdResponse>()
                .ForMember(d => d.TableCode, opt => opt.MapFrom(s =>
                (s.Area != null && !string.IsNullOrWhiteSpace(s.Area.CodePrefix)) ? s.Area.CodePrefix + "_" + s.TableNumber : s.TableNumber.ToString()))
                .ForMember(d => d.AreaName, opt => opt.MapFrom(s => s.Area.Name))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(s => s.Status.ToString()));
        }

    }
}
