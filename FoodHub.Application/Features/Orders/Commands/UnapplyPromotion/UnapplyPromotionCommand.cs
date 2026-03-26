using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Orders.Commands.UnapplyPromotion
{
    public sealed record UnapplyPromotionCommand(Guid OrderId) : IRequest<Result<Unit>>;
}
