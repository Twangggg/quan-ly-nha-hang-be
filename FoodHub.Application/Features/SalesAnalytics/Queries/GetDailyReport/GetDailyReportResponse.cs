namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport
{
    public class GetDailyReportResponse
    {
        /// <summary>Ngày báo cáo (định dạng VN).</summary>
        public DateOnly Date { get; set; }

        /// <summary>Tổng doanh thu các đơn Paid + Completed.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng doanh thu (%) so với trung bình N ngày trước.
        /// </summary>
        public double RevenueGrowth { get; set; }

        /// <summary>Tổng số đơn hàng Paid + Completed.</summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng số đơn hàng (%) so với trung bình N ngày trước.
        /// </summary>
        public double OrderGrowth { get; set; }

        /// <summary>Giá trị trung bình mỗi đơn hàng (TotalRevenue / TotalOrders).</summary>
        public decimal AvgOrderValue { get; set; }

        /// <summary>Số đơn hàng bị huỷ (Cancelled) trong ngày.</summary>
        public int CancelledOrders { get; set; }

        /// <summary>
        /// Tỷ lệ đạt mục tiêu doanh thu (%) so với DailyTarget.
        /// </summary>
        public double RevenueAchievement { get; set; }
    }
}
