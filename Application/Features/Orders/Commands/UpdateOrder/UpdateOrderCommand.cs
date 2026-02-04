using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.UpdateOrder
{
    public class UpdateOrderCommand : IRequest<Result<UpdateOrderCommandResponse>>
    {
        public Guid OrderId { get; set; }
        public string? Note { get; set; } = null;
        public OrderType OrderType { get; set; }
        public Guid? TableId { get; set; }
        public string? Reason { get; set; }
        public ICollection<UpdateOrderItemDto> OrderItems { get; set; } = new List<UpdateOrderItemDto>();
    }

    public class UpdateOrderItemDto
    {
        public Guid? OrderItemId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}
