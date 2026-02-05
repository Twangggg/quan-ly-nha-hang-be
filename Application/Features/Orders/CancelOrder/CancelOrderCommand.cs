using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Orders.CancelOrder
{
    public class CancelOrderCommand : IRequest<Result<bool>>
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string Reason { get; set; } = null!;
    }
}
