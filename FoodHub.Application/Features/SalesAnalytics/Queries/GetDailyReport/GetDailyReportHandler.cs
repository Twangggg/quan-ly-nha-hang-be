using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDailyReportHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetDailyReportHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetDailyReportHandler> logger,
            ICacheService cacheService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
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
                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTz)
                );

            var movingAvgDays = request.MovingAverageDays > 0 ? request.MovingAverageDays : 30;

            // Cache 
            var cacheKey = $"DailyReport_{reportDate:yyyy_MM_dd}";
            var cachedData = await _cacheService.GetAsync<GetDailyReportResponse>(
                cacheKey,
                cancellationToken
            );
            if (cachedData != null)
            {
                _logger.LogInformation(
                    "Return daily report from cache for date: {Date}",
                    reportDate
                );
                return Result<GetDailyReportResponse>.Success(cachedData);
            }

            _logger.LogInformation("Getting daily report for date from DB: {Date}", reportDate);

            // Chuyển ngày VN → UTC range ─────────────────────────────────
            var (startUtc, endUtc) = ToUtcRange(reportDate);

            _logger.LogInformation("Getting daily report for date: {Date}", reportDate);

            // Query aggregration trực tiếp trên DB ────────────────────────────────────
            var aggResult = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o => o.PaidAt >= startUtc && o.PaidAt < endUtc)
                .GroupBy(o => 1) // Fake group by to wrap aggregates
                .Select(g => new
                {
                    TotalRevenue = g.Where(o =>
                            o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed
                        )
                        .Sum(o => o.TotalAmount),
                    TotalOrders = g.Count(o =>
                        o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed
                    ),
                    CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled),
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalRevenue = aggResult?.TotalRevenue ?? 0m;
            var totalOrders = aggResult?.TotalOrders ?? 0;
            var cancelledOrders = aggResult?.CancelledOrders ?? 0;

            // Tính moving average target ─────────────────────────────────
            var (avgRevenue, avgOrders) = await CalculateMovingAveragesAsync(
                reportDate,
                movingAvgDays,
                cancellationToken
            );

            double revenueGrowth = 0;
            double orderGrowth = 0;
            decimal avgOrderValue = 0;
            double revenueAchievement = 0;

            if (avgRevenue.HasValue && avgRevenue.Value > 0)
            {
                revenueGrowth = Math.Round((double)((totalRevenue - avgRevenue.Value) / avgRevenue.Value * 100), 2);
            }

            if (avgOrders.HasValue && avgOrders.Value > 0)
            {
                orderGrowth = Math.Round((double)((totalOrders - avgOrders.Value) / avgOrders.Value * 100), 2);
            }

            if (totalOrders > 0)
            {
                avgOrderValue = Math.Round(totalRevenue / totalOrders, 2);
            }

            if (avgRevenue.HasValue && avgRevenue.Value > 0)
            {
                revenueAchievement = Math.Round((double)(totalRevenue / avgRevenue.Value * 100), 2);
            }

            _logger.LogInformation(
                "Daily report for {Date}: Revenue={Revenue}, Orders={Orders}, AvgRevenue={AvgRevenue}, AvgOrders={AvgOrders}, Growth={RevenueGrowth}%/{OrderGrowth}%",
                reportDate,
                totalRevenue,
                totalOrders,
                avgRevenue,
                avgOrders,
                revenueGrowth,
                orderGrowth
            );

            return Result<GetDailyReportResponse>.Success(
                new GetDailyReportResponse
                {
                    Date = reportDate,
                    TotalRevenue = totalRevenue,
                    RevenueGrowth = revenueGrowth,
                    TotalOrders = totalOrders,
                    OrderGrowth = orderGrowth,
                    AvgOrderValue = avgOrderValue,
                    CancelledOrders = cancelledOrders,
                    RevenueAchievement = revenueAchievement,
                }
            );
        }

        /// <summary>
        /// Tính moving average doanh thu và số đơn của N ngày trước reportDate (theo giờ VN).
        /// </summary>
        private async Task<(decimal? AvgRevenue, int? AvgOrders)> CalculateMovingAveragesAsync(
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

            // Hằng số múi giờ VN (UTC+7)
            const int vnOffsetHours = 7;

            // Lấy doanh thu và số đơn theo từng ngày VN
            var dailyData = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o =>
                    o.PaidAt >= windowStartUtc
                    && o.PaidAt < windowEndUtc
                    && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                )
                .GroupBy(o => o.PaidAt!.Value.AddHours(vnOffsetHours).Date)
                .Select(g => new
                {
                    DailyRevenue = g.Sum(o => o.TotalAmount),
                    DailyOrderCount = g.Count()
                })
                .ToListAsync(cancellationToken);

            if (!dailyData.Any())
            {
                return (null, null);
            }

            var avgRevenue = Math.Round(dailyData.Average(d => d.DailyRevenue), 2);
            var avgOrders = (int)Math.Round(dailyData.Average(d => d.DailyOrderCount), 2);

            return (avgRevenue, avgOrders);
        }

        /// <summary>
        /// Chuyển một ngày VN sang UTC range [start, end) để dùng trong WHERE clause.
        /// </summary>
        private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly date)
        {
            var startLocal = date.ToDateTime(TimeOnly.MinValue);
            var endLocal = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _vietnamTz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _vietnamTz);

            return (startUtc, endUtc);
        }
    }
}
