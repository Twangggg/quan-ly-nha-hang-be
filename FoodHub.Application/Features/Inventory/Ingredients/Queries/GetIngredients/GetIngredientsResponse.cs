using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients
{
    public class GetIngredientsResponse : IMapFrom<Ingredient>
    {
        public Guid IngredientId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal LowStockThreshold { get; set; }
        public bool UseDefaultLowStockThreshold { get; set; }
        public decimal CostPrice { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? InventoryGroupId { get; set; }
        public string? InventoryGroupName { get; set; }
    }
}
