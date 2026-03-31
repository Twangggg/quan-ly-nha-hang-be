using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;


namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusResponse : IMapFrom<SetMenu>
    {
        public Guid SetMenuId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; } // Only visible to Manager/Cashier
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<GetSetMenuItemResponse> Items { get; set; } = new();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<SetMenu, GetSetMenusResponse>()
                .ForMember(d => d.SetMenuId, opt => opt.MapFrom(s => s.SetMenuId))
                .ForMember(d => d.UpdatedAt, opt => opt.MapFrom(s => s.UpdatedAt ?? s.CreatedAt))
                .ForMember(
                    d => d.Items,
                    opt => opt.MapFrom(s => s.SetMenuItems)
                );

            profile.CreateMap<SetMenuItem, GetSetMenuItemResponse>()
                .ForMember(d => d.MenuItemName, opt => opt.MapFrom(s => s.MenuItem.Name));
        }
    }

    public class GetSetMenuItemResponse
    {
        public Guid SetMenuItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
