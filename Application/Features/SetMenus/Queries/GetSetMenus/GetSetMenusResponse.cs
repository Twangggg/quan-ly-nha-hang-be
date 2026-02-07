using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusResponse : IMapFrom<SetMenu>
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public SetType SetType { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; } // Only visible to Manager/Cashier
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<SetMenu, GetSetMenusResponse>()
                .ForMember(d => d.SetMenuId, opt => opt.MapFrom(s => s.SetMenuId))
                .ForMember(d => d.SetType, opt => opt.MapFrom(s => s.SetType))
                .ForMember(d => d.UpdatedAt, opt => opt.MapFrom(s => s.UpdatedAt ?? s.CreatedAt));
        }
    }
}
