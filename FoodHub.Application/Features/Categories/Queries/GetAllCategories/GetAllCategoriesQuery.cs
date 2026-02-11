using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public record GetAllCategoriesQuery(PaginationParams Pagination) : IRequest<Result<PagedResult<GetCategoriesResponse>>>;
}
