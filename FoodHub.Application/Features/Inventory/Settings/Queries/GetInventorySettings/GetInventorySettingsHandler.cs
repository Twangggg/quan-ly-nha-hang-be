using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings
{
    public class GetInventorySettingsHandler
        : IRequestHandler<GetInventorySettingsQuery, Result<GetInventorySettingsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetInventorySettingsHandler> _logger;

        public GetInventorySettingsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventorySettingsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<GetInventorySettingsResponse>> Handle(
            GetInventorySettingsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Start handling GetInventorySettings");

            var cachedResponse = await _cacheService.GetAsync<GetInventorySettingsResponse>(
                CacheKey.InventorySettings,
                cancellationToken
            );

            if (cachedResponse != null)
            {
                _logger.LogInformation("End handling GetInventorySettings from cache");
                return Result<GetInventorySettingsResponse>.Success(cachedResponse);
            }

            var repo = _unitOfWork.Repository<InventorySettings>();

            var settings = await repo.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                    cancellationToken
                );

            if (settings == null)
            {
                settings = InventorySettings.CreateDefault();
                await repo.AddAsync(settings);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }

            var response = MapToResponse(settings);

            await _cacheService.SetAsync(
                CacheKey.InventorySettings,
                response,
                CacheTTL.InventorySettings,
                cancellationToken
            );

            _logger.LogInformation("End handling GetInventorySettings");
            return Result<GetInventorySettingsResponse>.Success(response);
        }

        private static GetInventorySettingsResponse MapToResponse(InventorySettings settings)
        {
            return new GetInventorySettingsResponse
            {
                ExpiryWarningDays = settings.ExpiryWarningDays,
                DefaultLowStockThreshold = settings.DefaultLowStockThreshold,
                AutoDeductOnCompleted = settings.AutoDeductOnCompleted,
                CostMethod = settings.CostMethod,
                MaxCostRecalcDays = settings.MaxCostRecalcDays,
                OpeningStockImportCooldownHours = settings.OpeningStockImportCooldownHours,
                OpeningStockStatus = settings.OpeningStockStatus,
                LockedAt = settings.LockedAt,
                LastOpeningStockImportedAt = settings.LastOpeningStockImportedAt,
                NextOpeningStockImportAllowedAt = settings.GetNextOpeningStockImportAllowedAt(),
            };
        }
    }
}
