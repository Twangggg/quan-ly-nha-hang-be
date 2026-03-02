using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Commands.DeleteTable
{
    public class DeleteTableResponse : IMapFrom<Table>
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public Guid AreaId { get; set; } = Guid.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string AreaCodePrefix { get; set; } = string.Empty;
        public TableStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Table, DeleteTableResponse>()
                .ForMember(dest => dest.AreaName, opt => opt.MapFrom(src => src.Area != null ? src.Area.Name : string.Empty))
                .ForMember(dest => dest.AreaCodePrefix, opt => opt.MapFrom(src => src.Area != null ? src.Area.CodePrefix : string.Empty));
        }
    }
}
