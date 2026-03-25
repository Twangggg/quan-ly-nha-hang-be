using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Services
{
    public class InventoryRuleResolver : IInventoryRuleResolver
    {
        public InventoryResolvedRules Resolve(
            Ingredient ingredient,
            InventorySettings globalSettings
        )
        {
            var source = InventoryRuleSource.Global;

            decimal lowStockThreshold = globalSettings.DefaultLowStockThreshold;
            int expiryWarningDays = globalSettings.ExpiryWarningDays;
            InventoryCostMethod costMethod = globalSettings.CostMethod;

            if (ingredient.InventoryGroup is not null)
            {
                var group = ingredient.InventoryGroup;

                if (group.LowStockThreshold.HasValue)
                {
                    lowStockThreshold = group.LowStockThreshold.Value;
                    source = InventoryRuleSource.Group;
                }

                if (group.ExpiryWarningDays.HasValue)
                {
                    expiryWarningDays = group.ExpiryWarningDays.Value;
                    source = InventoryRuleSource.Group;
                }

                if (group.DefaultCostMethod.HasValue)
                {
                    costMethod = group.DefaultCostMethod.Value;
                    source = InventoryRuleSource.Group;
                }
            }

            if (!ingredient.UseDefaultLowStockThreshold)
            {
                lowStockThreshold = ingredient.LowStockThreshold;
                source = InventoryRuleSource.Ingredient;
            }

            return new InventoryResolvedRules(
                lowStockThreshold,
                expiryWarningDays,
                costMethod,
                source
            );
        }
    }

    public sealed record InventoryResolvedRules(
        decimal LowStockThreshold,
        int ExpiryWarningDays,
        InventoryCostMethod CostMethod,
        InventoryRuleSource Source
    );

    public enum InventoryRuleSource
    {
        Global = 1,
        Group = 2,
        Ingredient = 3,
    }
}
