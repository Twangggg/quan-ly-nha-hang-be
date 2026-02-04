using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public record CancelOrderCommand(
        Guid OrderId,
        string? Reason
    ) : IRequest<Result<bool>>;
}
