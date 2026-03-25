using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Promotions.Commands.DeletePromotion
{
    public sealed record DeletePromotionCommand(Guid PromotionId)
        : IRequest<Result<DeletePromotionResponse>>;
}
