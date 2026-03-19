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

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport
{
    public class GetMonthlyReportHandler
        : IRequestHandler<GetMonthlyReportQuery, Result<GetMonthlyReportResponse>>
    {
        // Múi giờ nhà hàng: Asia/Ho_Chi_Minh (+7)
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetMonthlyReportHandler> _logger;

        public GetMonthlyReportHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetMonthlyReportHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetMonthlyReportResponse>> Handle(
            GetMonthlyReportQuery request,
            CancellationToken cancellationToken
        )
        {
            // Xác định tháng/năm báo cáo (Mặc định là thời điểm hiện tại giờ VN)
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTz);
            var year = request.Year ?? nowVn.Year;
            var month = request.Month ?? nowVn.Month;

            // Đảm bảo month hợp lệ
            if (month < 1 || month > 12)
            {
                return Result<GetMonthlyReportResponse>.Failure(
                    "Invalid month. Must be between 1 and 12."
                );
            }

            _logger.LogInformation("Getting monthly report for {Month}/{Year}", month, year);

            // Chuyển khoảng thời gian tháng đó sang UTC để query DB
            var (startUtc, endUtc) = GetMonthlyUtcRange(year, month);

            // Query orders trong tháng (Dùng .Select() để tối ưu performance FFA-PERF)
            var allOrdersThisMonth = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o => o.PaidAt >= startUtc && o.PaidAt < endUtc)
                .Select(o => new { o.TotalAmount, o.Status })
                .ToListAsync(cancellationToken);

            var paidOrCompleted = allOrdersThisMonth
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                .ToList();

            var totalRevenue = paidOrCompleted.Sum(o => o.TotalAmount);
            var totalOrders = paidOrCompleted.Count;
            var cancelledOrders = allOrdersThisMonth.Count(o => o.Status == OrderStatus.Cancelled);

            _logger.LogInformation(
                "Monthly report for {Month}/{Year}: Revenue={Revenue}, Orders={Orders}",
                month,
                year,
                totalRevenue,
                totalOrders
            );

            return Result<GetMonthlyReportResponse>.Success(
                new GetMonthlyReportResponse
                {
                    Year = year,
                    Month = month,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    CancelledOrders = cancelledOrders,
                }
            );
        }

        /// <summary>
        /// Trả về khoảng [start, end) dưới dạng UTC của nguyên tháng theo giờ VN.
        /// end sẽ là 00:00:00 của ngày 1 tháng tiếp theo.
        /// </summary>
        private static (DateTime startUtc, DateTime endUtc) GetMonthlyUtcRange(int year, int month)
        {
            var firstDayVn = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(firstDayVn, _vietnamTz);

            var lastDayVn = firstDayVn.AddMonths(1);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(lastDayVn, _vietnamTz);

            return (startUtc, endUtc);
        }
    }
}
