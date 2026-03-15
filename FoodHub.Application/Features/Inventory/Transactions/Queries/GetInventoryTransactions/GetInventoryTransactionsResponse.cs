using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Transactions.Queries.GetInventoryTransactions
{
    public class GetInventoryTransactionsResponse
    {
        public Guid InventoryTransactionId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string IngredientCode { get; set; } = string.Empty;
        public InventoryTransactionType TransactionType { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Reference { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
