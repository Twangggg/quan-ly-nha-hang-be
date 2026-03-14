using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GenerateIngredientCode
{
    public record GenerateIngredientCodeQuery(string Name)
        : IRequest<Result<GenerateIngredientCodeResponse>>;
}
