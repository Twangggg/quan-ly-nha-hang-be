using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Promotions.Common;
using MediatR;

namespace FoodHub.Application.Features.Promotions.Queries.GetPromotions
{
    public record GetPromotionsQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<PromotionResponse>>>;
}
