using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    /// <summary>
    /// Merges a secondary active dine-in order into the primary active dine-in order.
    /// </summary>
    public record MergeOrderCommand(Guid FirstOrder, Guid SecondOrder) : IRequest<Result<MergeOrderResponse>>, IMustBeActive;
}
