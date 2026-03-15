using FoodHub.Application.Extensions.Mappings;

namespace FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity
{
    public class AdjustOrderItemQuantityResponse : IMapFrom<Domain.Entities.Order>
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
