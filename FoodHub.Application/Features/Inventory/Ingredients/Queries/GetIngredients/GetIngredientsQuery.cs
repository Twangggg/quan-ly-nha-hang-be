using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients
{
    public record GetIngredientsQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetIngredientsResponse>>>;
}
