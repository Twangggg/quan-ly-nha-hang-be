using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public class MergeOrderResponse
    {
        public Guid MergedOrderId { get; set; }
        public string MergedOrderCode { get; set; }
        public decimal MergedOrderTotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    }
}
