using System.Collections.Generic;

namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public class GetRecipeResponse
    {
        public Guid MenuItemId { get; set; }
        public string? Instructions { get; set; }
        public int PrepTimeMinutes { get; set; }
        public decimal TotalCost { get; set; }
        public List<GetRecipeItemResponse> Items { get; set; } = new();
    }
}
