using FoodHub.Application.Common.Models;
using MediatR;
using FoodHub.Application.Common.Security;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public record ChangeOrderTableCommand(Guid OrderId, Guid TableId) : IRequest<Result<ChangeOrderTableResponse>>, IMustBeActive;
}
