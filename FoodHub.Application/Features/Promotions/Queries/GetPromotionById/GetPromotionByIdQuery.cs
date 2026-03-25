using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Promotions.Common;
using MediatR;

namespace FoodHub.Application.Features.Promotions.Queries.GetPromotionById
{
    public record GetPromotionByIdQuery(Guid PromotionId) : IRequest<Result<PromotionResponse>>;
}
