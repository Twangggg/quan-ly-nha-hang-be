using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers
{
    public class GetBestSellersHandler
        : IRequestHandler<GetBestSellersQuery, Result<GetBestSellersResponse>>
    {
        private static readonly TimeZoneInfo _vietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetBestSellersHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetBestSellersHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetBestSellersHandler> logger,
            ICacheService cacheService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<GetBestSellersResponse>> Handle(
            GetBestSellersQuery request,
            CancellationToken cancellationToken
        )
        {
            var top = request.Top > 0 ? request.Top : 10;
            var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var startDate = request.StartDate ?? endDate.AddDays(-30);

            _logger.LogInformation(
                "Getting top {Top} best sellers from {StartDate} to {EndDate}",
                top,
                startDate,
                endDate
            );

            // Cache
            var cacheKey = $"BestSellers_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{top}";
            var cachedData = await _cacheService.GetAsync<GetBestSellersResponse>(
                cacheKey,
                cancellationToken
            );
            if (cachedData != null)
            {
                _logger.LogInformation(
                    "Return best sellers from cache for range {StartDate} to {EndDate}",
                    startDate,
                    endDate
                );
                return Result<GetBestSellersResponse>.Success(cachedData);
            }

            var ordersQuery = _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed);

            if (request.StartDate.HasValue)
            {
                var startVn = request.StartDate.Value.ToDateTime(TimeOnly.MinValue);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startVn, _vietnamTz);
                ordersQuery = ordersQuery.Where(o => o.PaidAt >= startUtc);
            }

            if (request.EndDate.HasValue)
            {
                var endVn = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endVn, _vietnamTz);
                ordersQuery = ordersQuery.Where(o => o.PaidAt <= endUtc);
            }

            var validOrdersQuery = ordersQuery.Select(o => o.OrderId);

            if (!await validOrdersQuery.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No orders found for best sellers report");
                return Result<GetBestSellersResponse>.Success(new GetBestSellersResponse());
            }

            var orderItemsQuery = _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .AsNoTracking()
                .Where(oi => validOrdersQuery.Contains(oi.OrderId))
                .Where(oi =>
                    oi.Status != OrderItemStatus.Cancelled && oi.Status != OrderItemStatus.Rejected
                );

            var totalRevenueAllRecords = await orderItemsQuery.SumAsync(
                oi =>
                    oi.Quantity
                    * (
                        oi.UnitPriceSnapshot
                        + oi.OptionGroups.SelectMany(og => og.OptionValues)
                            .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity)
                    ),
                cancellationToken
            );

            var bestSellersRaw = await orderItemsQuery
                .GroupBy(oi => new
                {
                    oi.MenuItemId,
                    oi.ItemNameSnapshot,
                    CategoryName = oi.MenuItem.Category.Name,
                    oi.MenuItem.CostPrice,
                })
                .Select(g => new
                {
                    ItemName = g.Key.ItemNameSnapshot,
                    CategoryName = g.Key.CategoryName,
                    CostPrice = g.Key.CostPrice,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    ItemTotalRevenue = g.Sum(x =>
                        x.Quantity
                        * (
                            x.UnitPriceSnapshot
                            + x.OptionGroups.SelectMany(og => og.OptionValues)
                                .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity)
                        )
                    ),
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ThenByDescending(x => x.ItemTotalRevenue)
                .Take(request.Top)
                .ToListAsync(cancellationToken);

            var bestSellers = bestSellersRaw
                .Select(x => new BestSellerDto
                {
                    ItemName = x.ItemName,
                    CategoryName = x.CategoryName,
                    QuantitySold = x.TotalQuantity,
                    TotalRevenue = x.ItemTotalRevenue,
                    RevenuePercentage =
                        totalRevenueAllRecords > 0
                            ? Math.Round(
                                (double)x.ItemTotalRevenue / (double)totalRevenueAllRecords * 100,
                                2
                            )
                            : 0,
                    GrossProfit = x.ItemTotalRevenue - (x.TotalQuantity * x.CostPrice),
                })
                .ToList();

            _logger.LogInformation(
                "Successfully retrieved {Count} best sellers",
                bestSellers.Count
            );

            var responseData = new GetBestSellersResponse { Items = bestSellers };

            // Set Cache: Nếu EndDate >= today thì cache 3 phút, ngược lại cache 1 tiếng
            var todayVn = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTz)
            );
            var cacheExpiration =
                endDate >= todayVn ? TimeSpan.FromMinutes(3) : TimeSpan.FromHours(1);
            await _cacheService.SetAsync(
                cacheKey,
                responseData,
                cacheExpiration,
                cancellationToken
            );

            return Result<GetBestSellersResponse>.Success(responseData);
        }
    }
}
