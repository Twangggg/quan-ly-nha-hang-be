namespace FoodHub.Application.Features.Dashboard.Orders.Queries.GetOrderDashboardOverview
{
    public class GetOrderDashboardOverviewResponse
    {
        public DateTime GeneratedAtUtc { get; set; }
        public int ActiveOrders { get; set; }
        public int PriorityOrders { get; set; }
        public int DineInOrders { get; set; }
        public int TakeawayOrders { get; set; }
        public int DeliveryOrders { get; set; }
        public int OccupiedTables { get; set; }
        public int AvailableTables { get; set; }
        public int PendingKitchenItems { get; set; }
        public int CookingItems { get; set; }
        public int CompletedItems { get; set; }
        public int WaitingCheckoutOrders { get; set; }
        public int TodayPaidOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<OrderDashboardStatusBreakdownItem> StatusBreakdown { get; set; } = new();
        public List<OrderDashboardTopOrderItem> TopActiveOrders { get; set; } = new();
    }

    public class OrderDashboardStatusBreakdownItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class OrderDashboardTopOrderItem
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? TableId { get; set; }
        public string? TableLabel { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPriority { get; set; }
        public int ItemCount { get; set; }
        public int FinishedItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
