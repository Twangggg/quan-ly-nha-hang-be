namespace FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe
{
    public class GetRecipeItemResponse
    {
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal QuantityPerServing { get; set; }
    }
}
