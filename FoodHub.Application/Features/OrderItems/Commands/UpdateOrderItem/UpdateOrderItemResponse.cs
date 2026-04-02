using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem
{
    public class UpdateOrderItemResponse : IMapFrom<Domain.Entities.Order>
    {
        public Guid OrderId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<UpdateOrderItemDto> Items { get; set; } = new List<UpdateOrderItemDto>();
    }
}
