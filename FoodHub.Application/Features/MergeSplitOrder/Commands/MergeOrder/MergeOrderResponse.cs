using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderResponse : IMapFrom<Order>
    {
        public Guid OrderId { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    }
}
