namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public class GetInventoryReportResponse
    {
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OpeningStock { get; set; }
        public decimal TotalStockIn { get; set; }
        public decimal TotalStockOut { get; set; }
        public decimal TotalSaleDeduction { get; set; }
        public decimal TotalOutbound { get; set; }
        public decimal ClosingStock { get; set; }
        public decimal AverageUnitCost { get; set; }
        public decimal ClosingStockValue { get; set; }
    }
}
