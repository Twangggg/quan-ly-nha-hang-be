using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Orders.Commands.CompleteOrder
{
    public class CompleteOrderResponse : IMapFrom<Domain.Entities.Order>
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public Guid? TableId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
