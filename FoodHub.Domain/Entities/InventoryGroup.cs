using System;
using System.Collections.Generic;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class InventoryGroup : BaseEntity
    {
        protected InventoryGroup() { }

        public Guid InventoryGroupId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public decimal? LowStockThreshold { get; private set; }
        public int? ExpiryWarningDays { get; private set; }
        public InventoryCostMethod? DefaultCostMethod { get; private set; }

        public virtual ICollection<Ingredient> Ingredients { get; private set; } =
            new List<Ingredient>();

        public static InventoryGroup Create(
            string name,
            string? description = null,
            decimal? lowStockThreshold = null,
            int? expiryWarningDays = null,
            InventoryCostMethod? defaultCostMethod = null,
            Guid? createdBy = null
        )
        {
            return new InventoryGroup
            {
                InventoryGroupId = Guid.NewGuid(),
                Name = name,
                Description = description,
                LowStockThreshold = lowStockThreshold,
                ExpiryWarningDays = expiryWarningDays,
                DefaultCostMethod = defaultCostMethod,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            string name,
            string? description,
            decimal? lowStockThreshold,
            int? expiryWarningDays,
            InventoryCostMethod? defaultCostMethod,
            Guid? updatedBy = null
        )
        {
            if (expiryWarningDays.HasValue && expiryWarningDays.Value < 1)
            {
                return DomainResult.Failure("InventoryGroup.InvalidExpiryWarningDays");
            }

            if (lowStockThreshold.HasValue && lowStockThreshold.Value < 0)
            {
                return DomainResult.Failure("InventoryGroup.InvalidLowStockThreshold");
            }

            Name = name;
            Description = description;
            LowStockThreshold = lowStockThreshold;
            ExpiryWarningDays = expiryWarningDays;
            DefaultCostMethod = defaultCostMethod;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }
    }
}
