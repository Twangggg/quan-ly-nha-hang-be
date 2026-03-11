using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder
{
    public record MergeOrderCommand(Guid FirstOrder, Guid SecondOrder) : IRequest<Result<MergeOrderResponse>>;
}
