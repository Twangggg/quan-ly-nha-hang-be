using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public class GetInventoryLedgerResponse
    {
        public DateTime OccurredAt { get; set; }
        public InventoryTransactionType TransactionType { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal QuantityDelta { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Note { get; set; }
    }
}
