using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Areas.Queries.GetPublicAreas
{
    public class GetPublicAreasResponse : IMapFrom<Area>
    {
        public Guid AreaId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public AreaType Type { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Area, GetPublicAreasResponse>();
        }
    }
}
