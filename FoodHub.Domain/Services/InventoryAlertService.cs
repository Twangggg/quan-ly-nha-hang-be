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
            int expiryWarningDays
        )
        {
            var outOfStockItems = ingredients
                .Where(x => x.CurrentStock == 0)
                .OrderBy(x => x.Name)
                .Select(x => new InventoryStockAlertSnapshot(
                    x.IngredientId,
                    x.Code,
                    x.Name,
                    x.BaseUnit,
                    x.CurrentStock,
                    x.LowStockThreshold
                ))
                .ToList();

            var lowStockItems = ingredients
                .Where(x => x.CurrentStock > 0 && x.CurrentStock <= x.LowStockThreshold)
                .OrderBy(x => x.CurrentStock)
                .ThenBy(x => x.Name)
                .Select(x => new InventoryStockAlertSnapshot(
                    x.IngredientId,
                    x.Code,
                    x.Name,
                    x.BaseUnit,
                    x.CurrentStock,
                    x.LowStockThreshold
                ))
                .ToList();

            var normalizedLots = lots
                .Where(x => x.DeletedAt == null && x.RemainingQuantity > 0)
                .Select(x =>
                {
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
        decimal Threshold
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
