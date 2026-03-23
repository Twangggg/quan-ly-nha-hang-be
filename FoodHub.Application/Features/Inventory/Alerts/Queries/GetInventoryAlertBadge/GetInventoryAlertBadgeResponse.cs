namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlertBadge
{
    public class GetInventoryAlertBadgeResponse
    {
        public int BadgeCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiredCount { get; set; }
        public int NearExpiryCount { get; set; }
    }
}
