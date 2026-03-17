using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public record UpsertRecipeCommand(Guid MenuItemId, List<UpsertRecipeItemDto> Items)
        : IRequest<Result<Unit>>;

    public class UpsertRecipeItemDto
    {
        public Guid IngredientId { get; set; }
        public decimal QuantityPerServing { get; set; }
    }
}
