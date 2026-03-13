using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public record SplitOrderCommand(
        Guid SourceOrderId,
        List<SplitOrderItemCommand> ItemsToSplit
    ) : IRequest<Result<SplitOrderResponse>>, IMustBeActive;

    public record SplitOrderItemCommand(
        Guid OrderItemId,
        int QuantityToSplit
    );
}
