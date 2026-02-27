using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
namespace FoodHub.Application.Features.Areas.Queries.GetAreaById
{
    public class GetAreaByIdResponse : IMapFrom<Area>
    {
        public Guid AreaId { get; set; }
        public required string Name { get; set; }
        public required string CodePrefix { get; set; }
        public AreaType Type { get; set; }
        public string? Description { get; set; }
        public AreaStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Area, GetAreaByIdResponse>();
        }
    }
}
