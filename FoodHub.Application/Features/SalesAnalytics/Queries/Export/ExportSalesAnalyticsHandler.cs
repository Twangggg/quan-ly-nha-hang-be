using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
using FoodHub.Application.Interfaces;
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
        private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
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
                reportTitle =
                    $"Báo cáo doanh thu từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}";
            }
            else
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
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

        private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly start, DateOnly end)
        {
            var startLocal = start.ToDateTime(TimeOnly.MinValue);
            var endLocal = end.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return (
                TimeZoneInfo.ConvertTimeToUtc(startLocal, VietnamTz),
                TimeZoneInfo.ConvertTimeToUtc(endLocal, VietnamTz)
            );
        }
    }
}
