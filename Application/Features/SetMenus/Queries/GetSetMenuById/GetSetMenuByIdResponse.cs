using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdResponse : IMapFrom<SetMenu>
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Items included in the set menu
        public List<SetMenuItemResponse>? Items { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<SetMenu, GetSetMenuByIdResponse>()
                .ForMember(d => d.SetMenuId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.SetMenuItems));
        }
    }

    public class SetMenuItemResponse : IMapFrom<SetMenuItem>
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<SetMenuItem, SetMenuItemResponse>()
                .ForMember(d => d.MenuItemName, opt => opt.MapFrom(s => s.MenuItem.Name));
        }
    }
}
