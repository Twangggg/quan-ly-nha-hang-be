using FoodHub.Domain.Entities;

namespace FoodHub.Domain.Services
{
    public interface IInventoryRuleResolver
    {
        InventoryResolvedRules Resolve(Ingredient ingredient, InventorySettings globalSettings);
    }
}
