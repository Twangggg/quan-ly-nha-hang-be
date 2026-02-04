using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Orders.Commands.UpdateOrder
{
    public class UpdateOrderCommandResponse : IMapFrom<Domain.Entities.Order>
    {
        public string? Note { get; set; } = null;
        public OrderType OrderType { get; set; }
        public ICollection<Domain.Entities.OrderItem> OrderItems { get; set; } = new List<Domain.Entities.OrderItem>();
    }
}
