using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public class SplitOrderResponse
    {
        public Guid SourceOrderId { get; set; }
        public string SourceOrderCode { get; set; } = null!;
        public decimal SourceOrderTotalAmount { get; set; }
        public List<SplitOrderItemDto> SourceOrderItems { get; set; } = new List<SplitOrderItemDto>();

        public Guid NewOrderId { get; set; }
        public string NewOrderCode { get; set; } = null!;
        public decimal NewOrderTotalAmount { get; set; }
        public List<SplitOrderItemDto> NewOrderItems { get; set; } = new List<SplitOrderItemDto>();
    }

    public class SplitOrderItemDto : IMapFrom<OrderItem>
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<OrderItem, SplitOrderItemDto>();
        }
    }
}
