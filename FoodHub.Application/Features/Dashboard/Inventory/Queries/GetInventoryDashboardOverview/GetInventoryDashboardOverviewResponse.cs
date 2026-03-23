namespace FoodHub.Application.Features.Dashboard.Inventory.Queries.GetInventoryDashboardOverview
{
    public class GetInventoryDashboardOverviewResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalIngredients { get; set; }
        public int ActiveIngredients { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiredLots { get; set; }
        public int NearExpiryLots { get; set; }
        public int BadgeCount { get; set; }
        public decimal TotalStockValue { get; set; }
        public decimal StockInToday { get; set; }
        public decimal StockOutToday { get; set; }
        public decimal SaleDeductionToday { get; set; }
        public List<InventoryDashboardStockAlertItem> TopLowStockItems { get; set; } = new();
        public List<InventoryDashboardExpiryItem> TopExpiringLots { get; set; } = new();
    }

    public class InventoryDashboardStockAlertItem
    {
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal Threshold { get; set; }
    }

    public class InventoryDashboardExpiryItem
    {
        public Guid InventoryLotId { get; set; }
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string LotCode { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal RemainingQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int? DaysRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
