using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.OrderItems.Common
{
    public class OrderItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }

        public string ItemCodeSnapshot { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;

        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public string? ItemNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public ICollection<OrderItemOptionGroupDto> OptionGroups { get; set; } =
            new List<OrderItemOptionGroupDto>();

        public void Mapping(Profile profile)
        {
            profile
                .CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
        }
    }
}
