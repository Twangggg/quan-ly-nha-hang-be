using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder
{
    /// <summary>
    /// Moves one or more active order items from a source dine-in order
    /// to an existing destination order or to a destination table.
    /// </summary>
    public record SplitOrderCommand(
        Guid SourceOrderId,
        Guid? DestinationOrderId,
        Guid? DestinationTableId,
        Guid? DestinationReservationId,
        List<SplitOrderItemCommand> ItemsToSplit
    ) : IRequest<Result<SplitOrderResponse>>, IMustBeActive;

    /// <summary>
    /// Describes a partial or full quantity transfer for a single order item.
    /// </summary>
    public record SplitOrderItemCommand(
        Guid OrderItemId,
        int QuantityToSplit
    );
}
