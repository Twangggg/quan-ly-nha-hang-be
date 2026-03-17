using System;
using System.Collections.Generic;
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
        public string BaseUnit { get; private set; } = string.Empty;
        public decimal CurrentStock { get; private set; }
        public decimal LowStockThreshold { get; private set; }
        public decimal CostPrice { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public virtual ICollection<IngredientUoMConversion> Conversions { get; private set; } = new List<IngredientUoMConversion>();
        public virtual ICollection<InventoryTransaction> InventoryTransactions
        {
            get;
            private set;
        } = new List<InventoryTransaction>();

        public static Ingredient Create(
            string code,
            string name,
            string baseUnit,
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
                BaseUnit = baseUnit,
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
            string baseUnit,
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
            BaseUnit = baseUnit;
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

        public DomainResult ReceiveStock(
            decimal quantity,
            decimal? unitCost = null,
            Guid? updatedBy = null
        )
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidStockInQuantity);
            }

            if (unitCost.HasValue && unitCost.Value < 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidStockInCost);
            }

            var previousStock = CurrentStock;
            var updatedStock = previousStock + quantity;

            if (unitCost.HasValue)
            {
                var previousValue = previousStock * CostPrice;
                var incomingValue = quantity * unitCost.Value;
                CostPrice = updatedStock == 0 ? 0 : (previousValue + incomingValue) / updatedStock;
            }

            CurrentStock = updatedStock;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult ReverseReceivedStock(
            decimal quantity,
            decimal? unitCost = null,
            Guid? updatedBy = null
        )
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidStockInQuantity);
            }

            if (CurrentStock - quantity < 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InsufficientStock);
            }

            var previousStock = CurrentStock;
            var updatedStock = previousStock - quantity;

            if (unitCost.HasValue)
            {
                if (updatedStock == 0)
                {
                    CostPrice = 0;
                }
                else
                {
                    var updatedValue = (previousStock * CostPrice) - (quantity * unitCost.Value);
                    CostPrice = updatedValue <= 0 ? 0 : updatedValue / updatedStock;
                }
            }

            CurrentStock = updatedStock;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult ReduceStock(decimal quantity, Guid? updatedBy = null)
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidStockInQuantity);
            }

            var previousStock = CurrentStock;
            if (previousStock - quantity < 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InsufficientStock);
            }

            var updatedStock = previousStock - quantity;

            CurrentStock = updatedStock;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult ReverseReducedStock(decimal quantity, Guid? updatedBy = null)
        {
            if (quantity <= 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidStockInQuantity);
            }

            var previousStock = CurrentStock;
            var updatedStock = previousStock + quantity;

            CurrentStock = updatedStock;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        public DomainResult SetOpeningStock(
            decimal quantity,
            decimal? costPrice = null,
            Guid? updatedBy = null
        )
        {
            if (quantity < 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidOpeningStockQuantity);
            }

            if (costPrice.HasValue && costPrice.Value < 0)
            {
                return DomainResult.Failure(DomainErrors.Ingredient.InvalidOpeningStockCost);
            }

            CurrentStock = quantity;

            if (costPrice.HasValue)
            {
                CostPrice = costPrice.Value;
            }

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

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
