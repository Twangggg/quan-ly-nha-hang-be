using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlertBadge
{
    public class GetInventoryAlertBadgeHandler
        : IRequestHandler<GetInventoryAlertBadgeQuery, Result<GetInventoryAlertBadgeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly InventoryAlertService _inventoryAlertService;
        private readonly ILogger<GetInventoryAlertBadgeHandler> _logger;

        public GetInventoryAlertBadgeHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            InventoryAlertService inventoryAlertService,
            ILogger<GetInventoryAlertBadgeHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _inventoryAlertService = inventoryAlertService;
            _logger = logger;
        }

        public async Task<Result<GetInventoryAlertBadgeResponse>> Handle(
            GetInventoryAlertBadgeQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventoryAlertBadge");

            const string cacheKey = "inventory:alerts:badge";
            var cached = await _cacheService.GetAsync<GetInventoryAlertBadgeResponse>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryAlertBadge from cache with BadgeCount={BadgeCount}",
                    cached.BadgeCount
                );
                return Result<GetInventoryAlertBadgeResponse>.Success(cached);
            }

            var settings =
                await _unitOfWork
                    .Repository<InventorySettings>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                        cancellationToken
                    )
                ?? InventorySettings.CreateDefault();

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

            var response = new GetInventoryAlertBadgeResponse
            {
                BadgeCount = summary.BadgeCount,
                OutOfStockCount = summary.OutOfStockItems.Count,
                LowStockCount = summary.LowStockItems.Count,
                ExpiredCount = summary.ExpiredLots.Count,
                NearExpiryCount = summary.NearExpiryLots.Count,
            };

            await _cacheService.SetAsync(
                cacheKey,
                response,
                TimeSpan.FromMinutes(5),
                cancellationToken
            );
            _logger.LogInformation(
                "End handling GetInventoryAlertBadge with BadgeCount={BadgeCount}",
                response.BadgeCount
            );

            return Result<GetInventoryAlertBadgeResponse>.Success(response);
        }
    }
}
