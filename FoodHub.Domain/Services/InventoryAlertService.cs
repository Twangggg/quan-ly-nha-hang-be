using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Services
{
    public class InventoryAlertService
    {
        public InventoryAlertSummary BuildSummary(
            IEnumerable<Ingredient> ingredients,
            IEnumerable<InventoryLot> lots,
            DateTime currentDate,
            int defaultExpiryWarningDays,
            InventoryRuleResolver ruleResolver,
            InventorySettings globalSettings
        )
        {
            var resolvedIngredients = ingredients
                .Select(ingredient => new
                {
                    Ingredient = ingredient,
                    Rules = ruleResolver.Resolve(ingredient, globalSettings)
                })
                .ToList();

            var outOfStockItems = resolvedIngredients
                .Where(x => x.Ingredient.CurrentStock == 0)
                .OrderBy(x => x.Ingredient.Name)
                .Select(x => new InventoryStockAlertSnapshot(
                    x.Ingredient.IngredientId,
                    x.Ingredient.Code,
                    x.Ingredient.Name,
                    x.Ingredient.BaseUnit,
                    x.Ingredient.CurrentStock,
                    x.Rules.LowStockThreshold,
                    x.Rules.Source
                ))
                .ToList();

            var lowStockItems = resolvedIngredients
                .Where(x => x.Ingredient.CurrentStock > 0 && x.Ingredient.CurrentStock <= x.Rules.LowStockThreshold)
                .OrderBy(x => x.Ingredient.CurrentStock)
                .ThenBy(x => x.Ingredient.Name)
                .Select(x => new InventoryStockAlertSnapshot(
                    x.Ingredient.IngredientId,
                    x.Ingredient.Code,
                    x.Ingredient.Name,
                    x.Ingredient.BaseUnit,
                    x.Ingredient.CurrentStock,
                    x.Rules.LowStockThreshold,
                    x.Rules.Source
                ))
                .ToList();

            var normalizedLots = lots
                .Where(x => x.DeletedAt == null && x.RemainingQuantity > 0)
                .Select(x =>
                {
                    var ingredientRules = resolvedIngredients.FirstOrDefault(r => r.Ingredient.IngredientId == x.IngredientId)?.Rules;
                    var expiryWarningDays = ingredientRules?.ExpiryWarningDays ?? defaultExpiryWarningDays;
                    x.RefreshStatus(currentDate, expiryWarningDays);
                    return x;
                })
                .ToList();

            var expiredLots = normalizedLots
                .Where(x => x.Status == InventoryLotStatus.Expired)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ReceivedAt)
                .Select(MapExpirySnapshot(currentDate))
                .ToList();

            var nearExpiryLots = normalizedLots
                .Where(x => x.Status == InventoryLotStatus.NearExpiry)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ReceivedAt)
                .Select(MapExpirySnapshot(currentDate))
                .ToList();

            return new InventoryAlertSummary(
                outOfStockItems,
                lowStockItems,
                expiredLots,
                nearExpiryLots,
                outOfStockItems.Count + lowStockItems.Count + expiredLots.Count + nearExpiryLots.Count
            );
        }

        private static Func<InventoryLot, InventoryExpiryAlertSnapshot> MapExpirySnapshot(DateTime currentDate)
        {
            return lot => new InventoryExpiryAlertSnapshot(
                lot.InventoryLotId,
                lot.IngredientId,
                lot.Ingredient.Code,
                lot.Ingredient.Name,
                lot.LotCode,
                lot.ExpiryDate,
                lot.RemainingQuantity,
                lot.Ingredient.BaseUnit,
                lot.ExpiryDate.HasValue ? (lot.ExpiryDate.Value.Date - currentDate.Date).Days : null,
                lot.Status
            );
        }
    }

    public sealed record InventoryAlertSummary(
        IReadOnlyList<InventoryStockAlertSnapshot> OutOfStockItems,
        IReadOnlyList<InventoryStockAlertSnapshot> LowStockItems,
        IReadOnlyList<InventoryExpiryAlertSnapshot> ExpiredLots,
        IReadOnlyList<InventoryExpiryAlertSnapshot> NearExpiryLots,
        int BadgeCount
    );

    public sealed record InventoryStockAlertSnapshot(
        Guid IngredientId,
        string IngredientCode,
        string IngredientName,
        string Unit,
        decimal CurrentStock,
        decimal Threshold,
        InventoryRuleSource Source
    );

    public sealed record InventoryExpiryAlertSnapshot(
        Guid InventoryLotId,
        Guid IngredientId,
        string IngredientCode,
        string IngredientName,
        string LotCode,
        DateTime? ExpiryDate,
        decimal RemainingQuantity,
        string Unit,
        int? DaysRemaining,
        InventoryLotStatus Status
    );
}
