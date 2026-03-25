using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlerts
{
    public class GetInventoryAlertsHandler
        : IRequestHandler<GetInventoryAlertsQuery, Result<GetInventoryAlertsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly InventoryAlertService _inventoryAlertService;
        private readonly ILogger<GetInventoryAlertsHandler> _logger;

        public GetInventoryAlertsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            InventoryAlertService inventoryAlertService,
            ILogger<GetInventoryAlertsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _inventoryAlertService = inventoryAlertService;
            _logger = logger;
        }

        public async Task<Result<GetInventoryAlertsResponse>> Handle(
            GetInventoryAlertsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventoryAlerts");

            const string cacheKey = "inventory:alerts:full";
            var cached = await _cacheService.GetAsync<GetInventoryAlertsResponse>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryAlerts from cache with BadgeCount={BadgeCount}",
                    cached.BadgeCount
                );
                return Result<GetInventoryAlertsResponse>.Success(cached);
            }

            var response = await BuildResponseAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);
            _logger.LogInformation(
                "End handling GetInventoryAlerts with BadgeCount={BadgeCount}",
                response.BadgeCount
            );

            return Result<GetInventoryAlertsResponse>.Success(response);
        }

        private async Task<GetInventoryAlertsResponse> BuildResponseAsync(
            CancellationToken cancellationToken
        )
        {
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
                .Include(x => x.InventoryGroup)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var lots = await _unitOfWork
                .Repository<InventoryLot>()
                .Query()
                .Include(x => x.Ingredient)
                .ToListAsync(cancellationToken);

            var summary = _inventoryAlertService.BuildSummary(
                ingredients,
                lots,
                DateTime.UtcNow.Date,
                settings.ExpiryWarningDays,
                new InventoryRuleResolver(),
                settings
            );

            return new GetInventoryAlertsResponse
            {
                BadgeCount = summary.BadgeCount,
                OutOfStockItems = summary.OutOfStockItems
                    .Select(
                        x =>
                            new InventoryStockAlertItemResponse
                            {
                                IngredientId = x.IngredientId,
                                IngredientCode = x.IngredientCode,
                                IngredientName = x.IngredientName,
                                Unit = x.Unit,
                                CurrentStock = x.CurrentStock,
                                Threshold = x.Threshold,
                                Source = (InventoryRuleSourceDto)x.Source,
                            }
                    )
                    .ToList(),
                LowStockItems = summary.LowStockItems
                    .Select(
                        x =>
                            new InventoryStockAlertItemResponse
                            {
                                IngredientId = x.IngredientId,
                                IngredientCode = x.IngredientCode,
                                IngredientName = x.IngredientName,
                                Unit = x.Unit,
                                CurrentStock = x.CurrentStock,
                                Threshold = x.Threshold,
                                Source = (InventoryRuleSourceDto)x.Source,
                            }
                    )
                    .ToList(),
                ExpiredLots = summary.ExpiredLots
                    .Select(
                        x =>
                            new InventoryExpiryAlertItemResponse
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
                                Status = x.Status,
                            }
                    )
                    .ToList(),
                NearExpiryLots = summary.NearExpiryLots
                    .Select(
                        x =>
                            new InventoryExpiryAlertItemResponse
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
                                Status = x.Status,
                            }
                    )
                    .ToList(),
            };
        }
    }
}
