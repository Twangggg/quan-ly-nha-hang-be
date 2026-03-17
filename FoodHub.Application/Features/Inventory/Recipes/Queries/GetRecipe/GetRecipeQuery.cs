using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public record GetRecipeQuery(Guid MenuItemId) : IRequest<Result<List<GetRecipeItemResponse>>>;
}
