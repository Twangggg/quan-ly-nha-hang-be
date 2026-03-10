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
        private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById(
            "Asia/Ho_Chi_Minh"
        );

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetBestSellersHandler> _logger;

        public GetBestSellersHandler(IUnitOfWork unitOfWork, ILogger<GetBestSellersHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetBestSellersResponse>> Handle(
            GetBestSellersQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Getting best sellers report");

            var ordersQuery = _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed);

            if (request.StartDate.HasValue)
            {
                var startVn = request.StartDate.Value.ToDateTime(TimeOnly.MinValue);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startVn, VietnamTz);
                ordersQuery = ordersQuery.Where(o => o.PaidAt >= startUtc);
            }

            if (request.EndDate.HasValue)
            {
                var endVn = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endVn, VietnamTz);
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

            return Result<GetBestSellersResponse>.Success(
                new GetBestSellersResponse { Items = bestSellers }
            );
        }
    }
}
