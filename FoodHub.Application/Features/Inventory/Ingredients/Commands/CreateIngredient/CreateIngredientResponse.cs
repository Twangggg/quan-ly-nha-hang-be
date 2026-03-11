using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public class CreateIngredientResponse : IMapFrom<Ingredient>
    {
        public Guid IngredientId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal LowStockThreshold { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
