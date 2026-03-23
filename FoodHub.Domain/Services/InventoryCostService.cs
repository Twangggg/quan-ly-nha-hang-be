using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Services
{
    public class InventoryCostService
    {
        public InventoryCostRecalculationResult Recalculate(
            IEnumerable<InventoryTransaction> openingTransactions,
            IEnumerable<StockInReceiptItem> stockInItems,
            IEnumerable<StockOutReceiptItem> stockOutItems,
            DateTime fromInclusive,
            DateTime toExclusive
        )
        {
            var openingEvents = openingTransactions
                .Select(x => new InventoryCostEvent(
                    x.IngredientId,
                    x.OccurredAt,
                    x.CreatedAt,
                    x.InventoryTransactionId,
                    InventoryCostEventType.OpeningStock,
                    x.Quantity,
                    x.UnitCost,
                    null
                ));

            var stockInEvents = stockInItems.Select(x => new InventoryCostEvent(
                x.IngredientId,
                x.StockInReceipt.ReceivedAt,
                x.CreatedAt,
                x.StockInReceiptItemId,
                InventoryCostEventType.StockIn,
                x.Quantity,
                x.UnitCost,
                null
            ));

            var stockOutEvents = stockOutItems.Select(x => new InventoryCostEvent(
                x.IngredientId,
                x.StockOutReceipt.StockOutDate,
                x.CreatedAt,
                x.StockOutReceiptItemId,
                InventoryCostEventType.StockOut,
                x.Quantity,
                x.UnitPrice,
                x
            ));

            var events = openingEvents
                .Concat(stockInEvents)
                .Concat(stockOutEvents)
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.SortId)
                .ToList();

            var states = new Dictionary<Guid, InventoryCostState>();
            var updates = new List<InventoryCostUpdate>();

            foreach (var item in events)
            {
                if (!states.TryGetValue(item.IngredientId, out var state))
                {
                    state = new InventoryCostState();
                    states[item.IngredientId] = state;
                }

                switch (item.EventType)
                {
                    case InventoryCostEventType.OpeningStock:
                    case InventoryCostEventType.StockIn:
                        ApplyInbound(state, item.Quantity, item.UnitCost);
                        break;
                    case InventoryCostEventType.StockOut:
                        var calculatedUnitCost = Math.Round(state.AverageCost, 2, MidpointRounding.AwayFromZero);
                        if (
                            item.StockOutItem is not null
                            && item.OccurredAt >= fromInclusive
                            && item.OccurredAt < toExclusive
                        )
                        {
                            var previousLineAmount = item.StockOutItem.LineAmount;
                            var recalculatedLineAmount = Math.Round(
                                item.StockOutItem.Quantity * calculatedUnitCost,
                                2,
                                MidpointRounding.AwayFromZero
                            );

                            updates.Add(
                                new InventoryCostUpdate(
                                    item.StockOutItem,
                                    calculatedUnitCost,
                                    recalculatedLineAmount,
                                    recalculatedLineAmount - previousLineAmount
                                )
                            );
                        }

                        state.QuantityOnHand -= item.Quantity;
                        if (state.QuantityOnHand < 0)
                        {
                            state.QuantityOnHand = 0;
                        }
                        break;
                }
            }

            return new InventoryCostRecalculationResult(updates);
        }

        private static void ApplyInbound(
            InventoryCostState state,
            decimal quantity,
            decimal? unitCost
        )
        {
            if (quantity <= 0)
            {
                return;
            }

            var inboundUnitCost = Math.Max(unitCost ?? 0, 0);
            var inboundValue = quantity * inboundUnitCost;
            var currentValue = state.QuantityOnHand * state.AverageCost;
            var updatedQuantity = state.QuantityOnHand + quantity;

            state.AverageCost = updatedQuantity == 0 ? 0 : (currentValue + inboundValue) / updatedQuantity;
            state.QuantityOnHand = updatedQuantity;
        }
    }

    public sealed record InventoryCostRecalculationResult(IReadOnlyList<InventoryCostUpdate> Updates)
    {
        public int UpdatedItemCount => Updates.Count;
        public decimal TotalDelta => Updates.Sum(x => x.DeltaAmount);
        public int UpdatedReceiptCount =>
            Updates.Select(x => x.StockOutItem.StockOutReceiptId).Distinct().Count();
        public int UpdatedIngredientCount =>
            Updates.Select(x => x.StockOutItem.IngredientId).Distinct().Count();
    }

    public sealed record InventoryCostUpdate(
        StockOutReceiptItem StockOutItem,
        decimal UnitCost,
        decimal LineAmount,
        decimal DeltaAmount
    );

    internal sealed class InventoryCostState
    {
        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }
    }

    internal sealed record InventoryCostEvent(
        Guid IngredientId,
        DateTime OccurredAt,
        DateTime CreatedAt,
        Guid SortId,
        InventoryCostEventType EventType,
        decimal Quantity,
        decimal? UnitCost,
        StockOutReceiptItem? StockOutItem
    );

    internal enum InventoryCostEventType
    {
        OpeningStock = 1,
        StockIn = 2,
        StockOut = 3,
    }
}
