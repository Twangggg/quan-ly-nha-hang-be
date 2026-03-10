using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport
{
    public class GetCategoryReportHandler
        : IRequestHandler<GetCategoryReportQuery, Result<GetCategoryReportResponse>>
    {
        private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCategoryReportHandler> _logger;

        public GetCategoryReportHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetCategoryReportHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetCategoryReportResponse>> Handle(
            GetCategoryReportQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Fetching Category Report from {StartDate} to {EndDate}",
                request.StartDate,
                request.EndDate
            );

            // Convert to UTC range
            DateTime? startUtc = request.StartDate.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(
                    request.StartDate.Value.ToDateTime(TimeOnly.MinValue),
                    VietnamTz
                )
                : null;

            DateTime? endUtc = request.EndDate.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(
                    request.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                    VietnamTz
                )
                : null;

            // Query OrderItems JOIN MenuItems JOIN Categories
            var itemsQuery = _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTracking()
                .Where(oi =>
                    oi.Order.Status == OrderStatus.Paid || oi.Order.Status == OrderStatus.Completed
                );

            if (startUtc.HasValue)
            {
                itemsQuery = itemsQuery.Where(oi => oi.Order.PaidAt >= startUtc.Value);
            }

            if (endUtc.HasValue)
            {
                itemsQuery = itemsQuery.Where(oi => oi.Order.PaidAt < endUtc.Value);
            }

            var categoryTotals = await itemsQuery
                .GroupBy(oi => oi.MenuItem.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(oi => oi.UnitPriceSnapshot * oi.Quantity),
                    ItemCount = g.Sum(oi => oi.Quantity),
                })
                .ToListAsync(cancellationToken);

            var totalRevenue = categoryTotals.Sum(c => c.TotalRevenue);

            var response = new GetCategoryReportResponse
            {
                Items = categoryTotals
                    .Select(c => new CategoryReportDto
                    {
                        CategoryName = c.CategoryName,
                        TotalRevenue = c.TotalRevenue,
                        ItemCount = c.ItemCount,
                        RevenuePercentage =
                            totalRevenue > 0
                                ? Math.Round((double)(c.TotalRevenue / totalRevenue * 100), 2)
                                : 0,
                    })
                    .OrderByDescending(c => c.TotalRevenue)
                    .ToList(),
            };

            _logger.LogInformation(
                "Successfully retrieved category report with {Count} categories, TotalRevenue={Total}",
                response.Items.Count,
                totalRevenue
            );

            return Result<GetCategoryReportResponse>.Success(response);
        }
    }
}
