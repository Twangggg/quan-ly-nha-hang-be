using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Dashboard.Inventory.Queries.GetInventoryDashboardOverview
{
    public class GetInventoryDashboardOverviewHandler
        : IRequestHandler<
            GetInventoryDashboardOverviewQuery,
            Result<GetInventoryDashboardOverviewResponse>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly InventoryAlertService _inventoryAlertService;
        private readonly ILogger<GetInventoryDashboardOverviewHandler> _logger;

        public GetInventoryDashboardOverviewHandler(
            IUnitOfWork unitOfWork,
            InventoryAlertService inventoryAlertService,
            ILogger<GetInventoryDashboardOverviewHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _inventoryAlertService = inventoryAlertService;
            _logger = logger;
        }

        public async Task<Result<GetInventoryDashboardOverviewResponse>> Handle(
            GetInventoryDashboardOverviewQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventoryDashboardOverview");

            var utcToday = DateTime.UtcNow.Date;

            var settings = await _unitOfWork
                .Repository<InventorySettings>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                    cancellationToken
                ) ?? InventorySettings.CreateDefault();

            var ingredients = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var lots = await _unitOfWork
                .Repository<InventoryLot>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Ingredient)
                .ToListAsync(cancellationToken);

            var transactions = await _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Where(x => x.OccurredAt.Date == utcToday)
                .ToListAsync(cancellationToken);

            var summary = _inventoryAlertService.BuildSummary(
                ingredients,
                lots,
                utcToday,
                settings.ExpiryWarningDays
            );

            var topLowStockItems = summary.OutOfStockItems
                .Concat(summary.LowStockItems)
                .OrderBy(x => x.CurrentStock)
                .ThenBy(x => x.Threshold)
                .Take(10)
                .Select(x => new InventoryDashboardStockAlertItem
                {
                    IngredientId = x.IngredientId,
                    IngredientCode = x.IngredientCode,
                    IngredientName = x.IngredientName,
                    Unit = x.Unit,
                    CurrentStock = x.CurrentStock,
                    Threshold = x.Threshold,
                })
                .ToList();

            var topExpiringLots = summary.ExpiredLots
                .Concat(summary.NearExpiryLots)
                .OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                .Take(10)
                .Select(x => new InventoryDashboardExpiryItem
                {
                    InventoryLotId = x.InventoryLotId,
                    IngredientId = x.IngredientId,
                    IngredientCode = x.IngredientCode,
                    IngredientName = x.IngredientName,
                    LotCode = x.LotCode,
                    ExpiryDate = x.ExpiryDate,
                    RemainingQuantity = x.RemainingQuantity,
                    Unit = x.Unit,
                    DaysRemaining = x.DaysRemaining,
                    Status = x.Status.ToString(),
                })
                .ToList();

            var response = new GetInventoryDashboardOverviewResponse
            {
                GeneratedAtUtc = DateTime.UtcNow,
                TotalIngredients = ingredients.Count,
                ActiveIngredients = ingredients.Count(x => x.IsActive),
                OutOfStockCount = summary.OutOfStockItems.Count,
                LowStockCount = summary.LowStockItems.Count,
                ExpiredLots = summary.ExpiredLots.Count,
                NearExpiryLots = summary.NearExpiryLots.Count,
                BadgeCount = summary.BadgeCount,
                TotalStockValue = ingredients.Sum(x => x.CurrentStock * x.CostPrice),
                StockInToday = transactions
                    .Where(x => x.TransactionType == InventoryTransactionType.StockIn)
                    .Sum(x => Math.Abs(x.Quantity)),
                StockOutToday = transactions
                    .Where(x => x.TransactionType == InventoryTransactionType.StockOut)
                    .Sum(x => Math.Abs(x.Quantity)),
                SaleDeductionToday = transactions
                    .Where(x => x.TransactionType == InventoryTransactionType.SaleDeduction)
                    .Sum(x => Math.Abs(x.Quantity)),
                TopLowStockItems = topLowStockItems,
                TopExpiringLots = topExpiringLots,
            };

            _logger.LogInformation(
                "End handling GetInventoryDashboardOverview with BadgeCount={BadgeCount} and TotalStockValue={TotalStockValue}",
                response.BadgeCount,
                response.TotalStockValue
            );

            return Result<GetInventoryDashboardOverviewResponse>.Success(response);
        }
    }
}
