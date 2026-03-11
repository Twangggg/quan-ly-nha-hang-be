using System;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class Ingredient : BaseEntity
    {
        private Ingredient() { } 

        public Guid IngredientId { get; private set; }
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string Unit { get; private set; } = string.Empty;
        public decimal CurrentStock { get; private set; }
        public decimal LowStockThreshold { get; private set; }
        public decimal CostPrice { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;

        public static Ingredient Create(
            string code,
            string name,
            string unit,
            decimal lowStockThreshold,
            string? description = null
        )
        {
            return new Ingredient
            {
                IngredientId = Guid.NewGuid(),
                Code = code,
                Name = name,
                Unit = unit,
                LowStockThreshold = lowStockThreshold,
                CurrentStock = 0,
                CostPrice = 0,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
        }

        public DomainResult Update(
            string name,
            string unit,
            decimal lowStockThreshold,
            string? description,
            bool isActive
        )
        {
            Name = name;
            Unit = unit;
            LowStockThreshold = lowStockThreshold;
            Description = description;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        public DomainResult UpdateStock(decimal quantity, decimal newCostPrice = 0)
        {
            if (CurrentStock + quantity < 0)
            {
                return DomainResult.Failure("Ingredient.InsufficientStock");
            }

            if (newCostPrice > 0)
            {
                CostPrice = newCostPrice;
            }

            CurrentStock += quantity;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        public DomainResult Deactivate(bool isUsedInRecipe)
        {
            if (isUsedInRecipe)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.UsedInRecipe);
            }

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public string StockStatus =>
            CurrentStock switch
            {
                0 => "Hết hàng",
                var stock when stock <= LowStockThreshold => "Sắp hết",
                _ => "Đủ hàng",
            };
    }
}
