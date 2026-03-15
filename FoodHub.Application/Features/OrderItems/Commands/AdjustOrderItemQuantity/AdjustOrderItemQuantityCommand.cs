using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity
{
    public class AdjustOrderItemQuantityCommand : IRequest<Result<AdjustOrderItemQuantityResponse>>
    {
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }
}
