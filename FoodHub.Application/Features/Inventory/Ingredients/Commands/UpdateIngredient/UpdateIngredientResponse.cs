using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient
{
    public class UpdateIngredientResponse : IMapFrom<Ingredient>
    {
        public Guid IngredientId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BaseUnit { get; set; } = string.Empty;
        public decimal LowStockThreshold { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal CostPrice { get; set; }
        public StockStatus StockStatus { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
