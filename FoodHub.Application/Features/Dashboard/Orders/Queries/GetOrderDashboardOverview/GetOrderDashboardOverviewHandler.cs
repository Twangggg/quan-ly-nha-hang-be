using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Dashboard.Orders.Queries.GetOrderDashboardOverview
{
    public class GetOrderDashboardOverviewHandler
        : IRequestHandler<GetOrderDashboardOverviewQuery, Result<GetOrderDashboardOverviewResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrderDashboardOverviewHandler> _logger;

        public GetOrderDashboardOverviewHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetOrderDashboardOverviewHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<GetOrderDashboardOverviewResponse>> Handle(
            GetOrderDashboardOverviewQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetOrderDashboardOverview");

            var utcToday = DateTime.UtcNow.Date;

            var orders = await _unitOfWork
                .Repository<Order>()
                .Query()
                .AsNoTracking()
                .Include(x => x.OrderItems)
                .Include(x => x.Table)
                    .ThenInclude(x => x!.Area)
                .ToListAsync(cancellationToken);

            var tables = await _unitOfWork
                .Repository<Table>()
                .Query()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var activeOrders = orders.Where(x => x.Status == OrderStatus.Serving).ToList();
            var occupiedTableIds = activeOrders
                .Where(x => x.TableId.HasValue)
                .Select(x => x.TableId!.Value)
                .Distinct()
                .ToHashSet();

            var response = new GetOrderDashboardOverviewResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                ActiveOrders = activeOrders.Count,
                PriorityOrders = activeOrders.Count(x => x.IsPriority),
                DineInOrders = activeOrders.Count(x => x.OrderType == OrderType.DineIn),
                TakeawayOrders = activeOrders.Count(x => x.OrderType == OrderType.Takeaway),
                DeliveryOrders = activeOrders.Count(x => x.OrderType == OrderType.Delivery),
                OccupiedTables = occupiedTableIds.Count,
                AvailableTables = tables.Count(x =>
                    !occupiedTableIds.Contains(x.TableId) && x.Status == TableStatus.Available
                ),
                PendingKitchenItems = activeOrders.Sum(x =>
                    x.GetPendingKitchenItems().Count(item => item.Status == OrderItemStatus.Preparing)
                ),
                CookingItems = activeOrders.Sum(x =>
                    x.GetPendingKitchenItems().Count(item => item.Status == OrderItemStatus.Cooking)
                ),
                CompletedItems = activeOrders.Sum(x =>
                    x.GetCountableKitchenItems().Count(item => item.Status == OrderItemStatus.Completed)
                ),
                WaitingCheckoutOrders = activeOrders.Count(x =>
                    x.OrderItems.Any() && !x.GetPendingKitchenItems().Any()
                ),
                TodayPaidOrders = orders.Count(x =>
                    x.PaidAt.HasValue && x.PaidAt.Value.Date == utcToday
                ),
                TodayRevenue = orders
                    .Where(x => x.PaidAt.HasValue && x.PaidAt.Value.Date == utcToday)
                    .Sum(x => x.TotalAmount),
                StatusBreakdown = orders
                    .GroupBy(x => x.Status)
                    .OrderBy(x => x.Key)
                    .Select(x => new OrderDashboardStatusBreakdownItem
                    {
                        Status = x.Key.ToString(),
                        Count = x.Count(),
                    })
                    .ToList(),
                TopActiveOrders = activeOrders
                    .OrderByDescending(x => x.IsPriority)
                    .ThenBy(x => x.CreatedAt)
                    .Take(10)
                    .Select(x => new OrderDashboardTopOrderItem
                    {
                        OrderId = x.OrderId,
                        OrderCode = x.OrderCode,
                        OrderType = x.OrderType.ToString(),
                        Status = x.Status.ToString(),
                        TableId = x.TableId,
                        TableLabel = BuildTableLabel(x),
                        TotalAmount = x.TotalAmount,
                        IsPriority = x.IsPriority,
                        ItemCount = x.GetCountableKitchenItems().Sum(item => item.Quantity),
                        FinishedItemCount = x.GetCountableKitchenItems().Count(item => item.IsFinished()),
                        CreatedAt = x.CreatedAt,
                    })
                    .ToList(),
            };

            _logger.LogInformation(
                "End handling GetOrderDashboardOverview with ActiveOrders={ActiveOrders} and TodayRevenue={TodayRevenue}",
                response.ActiveOrders,
                response.TodayRevenue
            );

            return Result<GetOrderDashboardOverviewResponse>.Success(response);
        }

        private static string? BuildTableLabel(Order order)
        {
            if (order.Table is null)
            {
                return null;
            }

            if (
                order.Table.Area is not null
                && !string.IsNullOrWhiteSpace(order.Table.Area.CodePrefix)
            )
            {
                return $"{order.Table.Area.CodePrefix}_{order.Table.TableNumber}";
            }

            return order.Table.TableNumber.ToString();
        }
    }
}
