namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport
{
    public class GetMonthlyReportResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }

        /// <summary>Tổng doanh thu các đơn Paid + Completed.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Tổng số đơn hàng Paid + Completed.</summary>
        public int TotalOrders { get; set; }

        /// <summary>Số đơn hàng bị huỷ (Cancelled) trong tháng.</summary>
        public int CancelledOrders { get; set; }
    }
}
