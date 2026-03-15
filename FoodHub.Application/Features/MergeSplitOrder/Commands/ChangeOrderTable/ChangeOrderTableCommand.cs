using FoodHub.Application.Common.Models;
using MediatR;
using FoodHub.Application.Common.Behaviors;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    /// <summary>
    /// Moves an active dine-in order to another available table.
    /// </summary>
    public record ChangeOrderTableCommand(Guid OrderId, Guid TableId) : IRequest<Result<ChangeOrderTableResponse>>, IMustBeActive;
}
