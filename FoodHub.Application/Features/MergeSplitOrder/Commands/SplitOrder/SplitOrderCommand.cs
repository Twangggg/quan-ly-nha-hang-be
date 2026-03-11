using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public record SplitOrderCommand(Guid OrderId) : IRequest<Result<SplitOrderResponse>>;
}
