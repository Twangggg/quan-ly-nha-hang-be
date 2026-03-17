using System.Collections.Generic;

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public class UpsertRecipeRequest
    {
        public List<UpsertRecipeItemDto> Items { get; set; } = new();
    }
}
