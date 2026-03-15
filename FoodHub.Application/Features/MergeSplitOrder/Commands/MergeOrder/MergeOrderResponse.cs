using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    /// <summary>
    /// Returns the consolidated order snapshot after a merge operation.
    /// </summary>
    public class MergeOrderResponse
    {
        public Guid MergedOrderId { get; set; }
        public string MergedOrderCode { get; set; } = string.Empty;
        public decimal MergedOrderTotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

    }

    /// <summary>
    /// Lightweight order item snapshot returned by merge responses.
    /// </summary>
    public class OrderItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PriceSnapshot { get; set; }
        public string ItemNote { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<OrderItem, OrderItemDto>()
                // AutoMapper sẽ tự động map GetTotalPrice() -> TotalPrice
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.GetTotalPrice()));
        }
    }
}
