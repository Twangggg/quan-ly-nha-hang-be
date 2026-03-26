using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    /// <summary>
    /// Moves an active dine-in order to another available table.
    /// </summary>
    public record ChangeOrderTableCommand(Guid OrderId, Guid TableId) : IRequest<Result<ChangeOrderTableResponse>>, IMustBeActive;
}
