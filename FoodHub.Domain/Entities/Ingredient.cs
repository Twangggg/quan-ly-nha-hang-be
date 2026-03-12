using System;
using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Ingredient : BaseEntity
    {
        protected Ingredient() { }

        public virtual Guid IngredientId { get; private set; }
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
            decimal currentStock,
            decimal costPrice,
            string? description = null,
            Guid? createdBy = null
        )
        {
            return new Ingredient
            {
                IngredientId = Guid.NewGuid(),
                Code = code,
                Name = name,
                Unit = unit,
                LowStockThreshold = lowStockThreshold,
                CurrentStock = currentStock,
                CostPrice = costPrice,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            string name,
            string unit,
            decimal lowStockThreshold,
            string? description,
            bool isActive,
            string code,
            decimal currentStock,
            decimal costPrice,
            Guid? updatedBy = null
        )
        {
            Name = name;
            Unit = unit;
            LowStockThreshold = lowStockThreshold;
            Description = description;
            IsActive = isActive;
            Code = code;
            CurrentStock = currentStock;
            CostPrice = costPrice;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

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

        public virtual DomainResult Deactivate(bool isUsedInRecipe)
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

        public StockStatus GetStockStatus()
        {
            return CurrentStock switch
            {
                0 => StockStatus.OutOfStock,
                var stock when stock <= LowStockThreshold => StockStatus.LowStock,
                _ => StockStatus.Normal,
            };
        }
    }
}
