using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class MenuItem : BaseEntity
    {
        public Guid MenuItemId { get; set; }
        public required string Code { get; set; }
        public int ItemNumber { get; set; }
        public required string Name { get; set; }
        public required string ImageUrl { get; set; }
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public Station Station { get; set; }
        public int ExpectedTime { get; set; } // Minutes

        public decimal Price { get; set; }
        public decimal CostPrice { get; set; } // Internal cost

        public bool IsOutOfStock { get; set; }

        public ICollection<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();
        public ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
        public virtual ICollection<MenuItemIngredient> Ingredients { get; set; } =
            new List<MenuItemIngredient>();

        public void UpdateCostFromIngredients(IEnumerable<MenuItemIngredient> ingredients)
        {
            decimal totalCost = 0;
            foreach (var item in ingredients)
            {
                // Ensure Ingredient is loaded to access CostPrice
                if (item.Ingredient != null)
                {
                    totalCost += item.Ingredient.CostPrice * item.QuantityPerServing;
                }
            }

            CostPrice = totalCost;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
