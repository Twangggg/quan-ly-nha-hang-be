using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem
{
    public record CancelOrderItemCommand(
        Guid OrderId,
        Guid OrderItemId,
        string? Reason,
        Domain.Entities.Order order
    ) : IRequest<Result<bool>>;
}
