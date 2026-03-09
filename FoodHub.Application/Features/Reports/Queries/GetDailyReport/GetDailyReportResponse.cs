namespace FoodHub.Application.Features.Reports.Queries.GetDailyReport
{
    public class GetDailyReportResponse
    {
        /// <summary>Ngày báo cáo (định dạng VN).</summary>
        public DateOnly Date { get; set; }

        /// <summary>Tổng doanh thu các đơn Paid + Completed.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Tổng số đơn hàng Paid + Completed.</summary>
        public int TotalOrders { get; set; }

        /// <summary>Số đơn hàng bị huỷ (Cancelled) trong ngày.</summary>
        public int CancelledOrders { get; set; }

        /// <summary>
        /// Mục tiêu doanh thu ngày (moving average N ngày trước).
        /// null nếu chưa có đủ dữ liệu lịch sử.
        /// </summary>
        public decimal? DailyTarget { get; set; }

        /// <summary>
        /// Tỷ lệ đạt mục tiêu (TotalRevenue / DailyTarget * 100).
        /// null nếu DailyTarget là null.
        /// </summary>
        public double? AchievementRate { get; set; }
    }
}
