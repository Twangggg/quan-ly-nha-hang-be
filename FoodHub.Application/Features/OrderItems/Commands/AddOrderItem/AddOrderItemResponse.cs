using FoodHub.Application.Extensions.Mappings;

namespace FoodHub.Application.Features.OrderItems.Commands.AddOrderItem
{
    public class AddOrderItemResponse : IMapFrom<Domain.Entities.Order>
    {
        public Guid OrderId { get; set; }
        public Guid NewOrderItemId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
