using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
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

namespace FoodHub.Application.Features.SalesAnalytics.Queries.Export
{
    public class ExportSalesAnalyticsHandler
        : IRequestHandler<ExportSalesAnalyticsQuery, Result<byte[]>>
    {
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ISalesExcelService _excelService;

        private readonly ILogger<ExportSalesAnalyticsHandler> _logger;

        public ExportSalesAnalyticsHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ISalesExcelService excelService,
            ILogger<ExportSalesAnalyticsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _excelService = excelService;
            _logger = logger;
        }

        public async Task<Result<byte[]>> Handle(
            ExportSalesAnalyticsQuery request,
            CancellationToken cancellationToken
        )
        {
            DateOnly startDate;
            DateOnly endDate;
            string reportTitle;

            // Determine Date Range
            if (request.Date.HasValue)
            {
                startDate = request.Date.Value;
                endDate = request.Date.Value;
                reportTitle = $"Báo cáo doanh thu ngày {startDate:dd/MM/yyyy}";
            }
            else if (request.Year.HasValue && request.Month.HasValue)
            {
                startDate = new DateOnly(request.Year.Value, request.Month.Value, 1);
                endDate = new DateOnly(
                    request.Year.Value,
                    request.Month.Value,
                    DateTime.DaysInMonth(request.Year.Value, request.Month.Value)
                );
                reportTitle = $"Báo cáo doanh thu tháng {request.Month}/{request.Year}";
            }
            else if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                startDate = request.StartDate.Value;
                endDate = request.EndDate.Value;
                reportTitle = BuildRangeReportTitle(startDate, endDate);
            }
            else
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTz);
                startDate = DateOnly.FromDateTime(now);
                endDate = startDate;
                reportTitle = $"Báo cáo doanh thu ngày {startDate:dd/MM/yyyy}";
            }

            // Fetch Summary Data (Calculated manually for range flexibility)
            _logger.LogInformation(
                "Starting export sales analytics report for range: {StartDate} to {EndDate}",
                startDate,
                endDate
            );

            var (startUtc, endUtc) = ToUtcRange(startDate, endDate);
            var ordersInRange = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o => o.PaidAt >= startUtc && o.PaidAt < endUtc)
                .Select(o => new { o.Status, o.TotalAmount })
                .ToListAsync(cancellationToken);

            var paidOrCompleted = ordersInRange
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                .ToList();

            var summary = new GetDailyReportResponse
            {
                Date = startDate,
                TotalRevenue = paidOrCompleted.Sum(o => o.TotalAmount),
                TotalOrders = paidOrCompleted.Count,
                CancelledOrders = ordersInRange.Count(o => o.Status == OrderStatus.Cancelled),
            };

            // Fetch Best Sellers & Categories using existing Handlers
            var bestSellersResult = await _mediator.Send(
                new GetBestSellersQuery
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Top = 100,
                },
                cancellationToken
            );

            var categoriesResult = await _mediator.Send(
                new GetCategoryReportQuery { StartDate = startDate, EndDate = endDate },
                cancellationToken
            );

            var fileContent = _excelService.ExportAnalyticsToExcel(
                reportTitle,
                summary,
                bestSellersResult.Data?.Items ?? new(),
                categoriesResult.Data?.Items ?? new()
            );

            _logger.LogInformation(
                "Sales analytics exported successfully, file size: {Size} bytes",
                fileContent.Length
            );

            return Result<byte[]>.Success(fileContent);
        }

        private static string BuildRangeReportTitle(DateOnly startDate, DateOnly endDate)
        {
            if (TryGetQuarter(startDate, endDate, out var quarter, out var year))
            {
                return $"Báo cáo doanh thu quý {quarter}/{year}";
            }

            return $"Báo cáo doanh thu {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        }

        private static bool TryGetQuarter(
            DateOnly startDate,
            DateOnly endDate,
            out int quarter,
            out int year
        )
        {
            quarter = 0;
            year = startDate.Year;

            if (startDate.Year != endDate.Year)
            {
                return false;
            }

            if (
                startDate == new DateOnly(startDate.Year, 1, 1)
                && endDate == new DateOnly(startDate.Year, 3, 31)
            )
            {
                quarter = 1;
                return true;
            }

            if (
                startDate == new DateOnly(startDate.Year, 4, 1)
                && endDate == new DateOnly(startDate.Year, 6, 30)
            )
            {
                quarter = 2;
                return true;
            }

            if (
                startDate == new DateOnly(startDate.Year, 7, 1)
                && endDate == new DateOnly(startDate.Year, 9, 30)
            )
            {
                quarter = 3;
                return true;
            }

            if (
                startDate == new DateOnly(startDate.Year, 10, 1)
                && endDate == new DateOnly(startDate.Year, 12, 31)
            )
            {
                quarter = 4;
                return true;
            }

            return false;
        }

        private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly start, DateOnly end)
        {
            var startLocal = start.ToDateTime(TimeOnly.MinValue);
            var endLocal = end.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return (
                TimeZoneInfo.ConvertTimeToUtc(startLocal, _vietnamTz),
                TimeZoneInfo.ConvertTimeToUtc(endLocal, _vietnamTz)
            );
        }
    }
}
