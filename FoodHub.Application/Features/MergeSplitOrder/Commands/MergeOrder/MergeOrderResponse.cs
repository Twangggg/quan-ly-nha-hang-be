using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderResponse
    {
        public Guid MergedOrderId { get; set; }
        public string MergedOrderCode { get; set; }
        public decimal MergedOrderTotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

    }

    public class OrderItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public string ItemNameSnapshot { get; set; }
        public int Quantity { get; set; }
        public decimal PriceSnapshot { get; set; }
        public string ItemNote { get; set; }
        public decimal TotalPrice { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OrderItem, OrderItemDto>()
                // AutoMapper sẽ tự động map GetTotalPrice() -> TotalPrice
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.GetTotalPrice()));
        }
    }
}
