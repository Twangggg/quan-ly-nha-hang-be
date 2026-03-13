using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public record ChangeOrderTableCommand(Guid OrderId, Guid TableId): IRequest<Result<ChangeOrderTableResponse>>;
}
