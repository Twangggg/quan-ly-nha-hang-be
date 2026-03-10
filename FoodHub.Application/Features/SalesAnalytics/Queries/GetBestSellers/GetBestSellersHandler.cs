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

            var validOrderIds = await ordersQuery
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);

            if (!validOrderIds.Any())
            {
                _logger.LogInformation("No orders found for best sellers report");
                return Result<GetBestSellersResponse>.Success(new GetBestSellersResponse());
            }

            // Optimize: Query order items only for valid orders, select only needed fields
            var orderItemsList = await _unitOfWork
                .Repository<OrderItem>()
                .Query()
                .Where(oi => validOrderIds.Contains(oi.OrderId))
                .Where(oi =>
                    oi.Status != OrderItemStatus.Cancelled && oi.Status != OrderItemStatus.Rejected
                )
                .Select(oi => new
                {
                    oi.MenuItemId,
                    oi.ItemNameSnapshot,
                    oi.Quantity,
                    oi.UnitPriceSnapshot,
                    OptionsTotal = oi
                        .OptionGroups.SelectMany(og => og.OptionValues)
                        .Sum(ov => ov.ExtraPriceSnapshot * ov.Quantity),
                    CostPrice = oi.MenuItem.CostPrice, // Current cost price
                    CategoryName = oi.MenuItem.Category.Name,
                })
                .ToListAsync(cancellationToken);

            var totalRevenueAllRecords = orderItemsList.Sum(oi =>
                oi.Quantity * (oi.UnitPriceSnapshot + oi.OptionsTotal)
            );

            var bestSellers = orderItemsList
                .GroupBy(oi => oi.MenuItemId)
                .Select(g =>
                {
                    var firstItem = g.First();
                    var totalQuantity = g.Sum(x => x.Quantity);
                    var itemTotalRevenue = g.Sum(x =>
                        x.Quantity * (x.UnitPriceSnapshot + x.OptionsTotal)
                    );
                    var totalCost = totalQuantity * firstItem.CostPrice;
                    var grossProfit = itemTotalRevenue - totalCost;
                    var revenuePercentage =
                        totalRevenueAllRecords > 0
                            ? (double)itemTotalRevenue / (double)totalRevenueAllRecords * 100
                            : 0;

                    return new BestSellerDto
                    {
                        ItemName = firstItem.ItemNameSnapshot,
                        CategoryName = firstItem.CategoryName,
                        QuantitySold = totalQuantity,
                        TotalRevenue = itemTotalRevenue,
                        RevenuePercentage = Math.Round(revenuePercentage, 2),
                        GrossProfit = grossProfit,
                    };
                })
                .OrderByDescending(x => x.QuantitySold)
                .ThenByDescending(x => x.TotalRevenue)
                .Take(request.Top)
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
