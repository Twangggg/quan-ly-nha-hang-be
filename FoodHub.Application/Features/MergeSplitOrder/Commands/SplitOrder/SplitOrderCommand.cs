using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    public record SplitOrderCommand(
        Guid SourceOrderId,
        List<SplitOrderItemDto> ItemsToSplit
    ) : IRequest<Result<SplitOrderResponse>>, IMustBeActive;

    public record SplitOrderItemDto(
        Guid OrderItemId,
        int QuantityToSplit
    );
}
