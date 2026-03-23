using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlerts
{
    public class GetInventoryAlertsResponse
    {
        public List<InventoryStockAlertItemResponse> OutOfStockItems { get; set; } = new();
        public List<InventoryStockAlertItemResponse> LowStockItems { get; set; } = new();
        public List<InventoryExpiryAlertItemResponse> ExpiredLots { get; set; } = new();
        public List<InventoryExpiryAlertItemResponse> NearExpiryLots { get; set; } = new();
        public int BadgeCount { get; set; }
    }

    public class InventoryStockAlertItemResponse
    {
        public Guid IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal Threshold { get; set; }
    }

    public class InventoryExpiryAlertItemResponse
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
        public InventoryLotStatus Status { get; set; }
    }
}
