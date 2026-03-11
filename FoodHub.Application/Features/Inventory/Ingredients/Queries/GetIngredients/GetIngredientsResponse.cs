using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients
{
    public class GetIngredientsResponse : IMapFrom<Ingredient>
    {
        public Guid IngredientId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal LowStockThreshold { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
