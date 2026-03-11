using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public class SplitOrderResponse
    {
        public Guid SourceOrderId { get; set; }
        public string SourceOrderCode { get; set; } = null!;
        public decimal SourceOrderTotalAmount { get; set; }
        public List<OrderItem> SourceOrderItems { get; set; } = new List<OrderItem>();

        public Guid NewOrderId { get; set; }
        public string NewOrderCode { get; set; } = null!;
        public decimal NewOrderTotalAmount { get; set; }
        public List<OrderItem> NewOrderItems { get; set; } = new List<OrderItem>();
    }
}
