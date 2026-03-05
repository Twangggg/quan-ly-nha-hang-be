using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTableStatus
{
    public class UpdateTableStatusResponse : IMapFrom<Table>
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public Guid AreaId { get; set; } = Guid.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string AreaCodePrefix { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile
                .CreateMap<Table, UpdateTableStatusResponse>()
                .ForMember(
                    d => d.TableCode,
                    opt =>
                        opt.MapFrom(s =>
                            (s.Area != null && !string.IsNullOrWhiteSpace(s.Area.CodePrefix))
                                ? s.Area.CodePrefix + "_" + s.TableNumber
                                : s.TableNumber.ToString()
                        )
                )
                .ForMember(
                    dest => dest.AreaName,
                    opt => opt.MapFrom(src => src.Area != null ? src.Area.Name : string.Empty)
                )
                .ForMember(
                    dest => dest.AreaCodePrefix,
                    opt => opt.MapFrom(src => src.Area != null ? src.Area.CodePrefix : string.Empty)
                )
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
