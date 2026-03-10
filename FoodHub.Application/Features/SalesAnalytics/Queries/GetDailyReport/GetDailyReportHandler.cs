using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport
{
    public class GetDailyReportHandler
        : IRequestHandler<GetDailyReportQuery, Result<GetDailyReportResponse>>
    {
        // Múi giờ nhà hàng: Asia/Ho_Chi_Minh (+7)
        private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDailyReportHandler> _logger;

        public GetDailyReportHandler(IUnitOfWork unitOfWork, ILogger<GetDailyReportHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetDailyReportResponse>> Handle(
            GetDailyReportQuery request,
            CancellationToken cancellationToken
        )
        {
            // Xác định ngày báo cáo theo giờ VN ──────────────────────────
            var reportDate =
                request.Date
                ?? DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz)
                );

            var movingAvgDays = request.MovingAverageDays > 0 ? request.MovingAverageDays : 30;

            // Chuyển ngày VN → UTC range ─────────────────────────────────
            var (startUtc, endUtc) = ToUtcRange(reportDate);

            _logger.LogInformation("Getting daily report for date: {Date}", reportDate);

            // Query orders trong ngày ────────────────────────────────────
            var allOrdersToday = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.PaidAt >= startUtc && o.PaidAt < endUtc)
                .Select(o => new { o.TotalAmount, o.Status })
                .ToListAsync(cancellationToken);

            var paidOrCompleted = allOrdersToday
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                .ToList();

            var totalRevenue = paidOrCompleted.Sum(o => o.TotalAmount);
            var totalOrders = paidOrCompleted.Count;
            var cancelledOrders = allOrdersToday.Count(o => o.Status == OrderStatus.Cancelled);

            // Tính moving average target ─────────────────────────────────
            decimal? dailyTarget = await CalculateMovingAverageAsync(
                reportDate,
                movingAvgDays,
                cancellationToken
            );

            double? achievementRate =
                dailyTarget.HasValue && dailyTarget.Value > 0
                    ? Math.Round((double)(totalRevenue / dailyTarget.Value * 100), 2)
                    : null;

            _logger.LogInformation(
                "Daily report for {Date}: Revenue={Revenue}, Orders={Orders}, Target={Target}",
                reportDate,
                totalRevenue,
                totalOrders,
                dailyTarget
            );

            return Result<GetDailyReportResponse>.Success(
                new GetDailyReportResponse
                {
                    Date = reportDate,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    CancelledOrders = cancelledOrders,
                    DailyTarget = dailyTarget,
                    AchievementRate = achievementRate,
                }
            );
        }

        /// <summary>
        /// Tính moving average doanh thu của N ngày trước reportDate (theo giờ VN).
        /// </summary>
        private async Task<decimal?> CalculateMovingAverageAsync(
            DateOnly reportDate,
            int movingAvgDays,
            CancellationToken cancellationToken
        )
        {
            // Window: [reportDate - N, reportDate - 1] (giờ VN)
            var windowStart = reportDate.AddDays(-movingAvgDays);
            var windowEnd = reportDate.AddDays(-1);

            var (windowStartUtc, _) = ToUtcRange(windowStart);
            var (_, windowEndUtc) = ToUtcRange(windowEnd);

            // Lấy tất cả đơn Paid/Completed trong window
            var ordersInWindow = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o =>
                    o.PaidAt >= windowStartUtc
                    && o.PaidAt < windowEndUtc
                    && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                )
                .Select(o => new { o.PaidAt, o.TotalAmount })
                .ToListAsync(cancellationToken);

            if (!ordersInWindow.Any())
                return null;

            // Group by ngày VN để tính daily revenue
            var dailyRevenues = ordersInWindow
                .GroupBy(o =>
                    DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt!.Value, VietnamTz)
                    )
                )
                .Select(g => g.Sum(o => o.TotalAmount))
                .ToList();

            return dailyRevenues.Any() ? Math.Round(dailyRevenues.Average(), 0) : null;
        }

        /// <summary>
        /// Chuyển một ngày VN sang UTC range [start, end) để dùng trong WHERE clause.
        /// </summary>
        private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly date)
        {
            var startLocal = date.ToDateTime(TimeOnly.MinValue);
            var endLocal = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, VietnamTz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, VietnamTz);

            return (startUtc, endUtc);
        }
    }
}
