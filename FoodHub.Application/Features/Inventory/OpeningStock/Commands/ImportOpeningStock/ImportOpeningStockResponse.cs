namespace FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock
{
    public class ImportOpeningStockResponse
    {
        public int UpdatedCount { get; set; }
        public int TransactionCount { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public DateTime? LastOpeningStockImportedAt { get; set; }
        public DateTime? NextOpeningStockImportAllowedAt { get; set; }
    }
}
