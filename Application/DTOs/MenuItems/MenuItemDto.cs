using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.DTOs.MenuItems
{
    public class MenuItemDto : IMapFrom<MenuItem>
    {
        public Guid MenuItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Station { get; set; }
        public int? ExpectedTime { get; set; }
        public decimal PriceDineIn { get; set; }
        public decimal? PriceTakeAway { get; set; }
        public decimal? Cost { get; set; } // Only visible to Manager/Cashier
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<MenuItem, MenuItemDto>()
                .ForMember(d => d.MenuItemId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Station, opt => opt.MapFrom(s => (int)s.Station))
                .ForMember(d => d.UpdatedAt, opt => opt.MapFrom(s => s.UpdatedAt ?? s.CreatedAt));
        }
    }
}
