namespace FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs
{
    public class RecalculateCogsResponse
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int ProcessedIngredients { get; set; }
        public int UpdatedReceipts { get; set; }
        public int UpdatedItems { get; set; }
        public decimal TotalAdjustmentAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
