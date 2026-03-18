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

            var response = await repo.Query()
                .AsNoTracking()
                .Where(x => x.SettingsKey == InventorySettings.DefaultSettingsKey)
                .Select(MapToResponse())
                .FirstOrDefaultAsync(cancellationToken);

            if (response == null)
            {
                var settings = InventorySettings.CreateDefault();
                await repo.AddAsync(settings);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                response = new GetInventorySettingsResponse
                {
                    ExpiryWarningDays = settings.ExpiryWarningDays,
                    DefaultLowStockThreshold = settings.DefaultLowStockThreshold,
                    AutoDeductOnCompleted = settings.AutoDeductOnCompleted,
                    CostMethod = settings.CostMethod,
                    MaxCostRecalcDays = settings.MaxCostRecalcDays,
                    OpeningStockStatus = settings.OpeningStockStatus,
                    LockedAt = settings.LockedAt,
                };
            }

            await _cacheService.SetAsync(
                CacheKey.InventorySettings,
                response,
                CacheTTL.InventorySettings,
                cancellationToken
            );

            _logger.LogInformation("End handling GetInventorySettings");
            return Result<GetInventorySettingsResponse>.Success(response);
        }

        private static System.Linq.Expressions.Expression<
            Func<InventorySettings, GetInventorySettingsResponse>
        > MapToResponse()
        {
            return x => new GetInventorySettingsResponse
            {
                ExpiryWarningDays = x.ExpiryWarningDays,
                DefaultLowStockThreshold = x.DefaultLowStockThreshold,
                AutoDeductOnCompleted = x.AutoDeductOnCompleted,
                CostMethod = x.CostMethod,
                MaxCostRecalcDays = x.MaxCostRecalcDays,
                OpeningStockStatus = x.OpeningStockStatus,
                LockedAt = x.LockedAt,
            };
        }
    }
}
