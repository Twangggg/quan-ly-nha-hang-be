using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings
{
    public class UpdateInventorySettingsHandler
        : IRequestHandler<UpdateInventorySettingsCommand, Result<UpdateInventorySettingsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateInventorySettingsHandler> _logger;

        public UpdateInventorySettingsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateInventorySettingsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateInventorySettingsResponse>> Handle(
            UpdateInventorySettingsCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling UpdateInventorySettings with ExpiryWarningDays={ExpiryWarningDays}, DefaultLowStockThreshold={DefaultLowStockThreshold}, AutoDeductOnCompleted={AutoDeductOnCompleted}, MaxCostRecalcDays={MaxCostRecalcDays}",
                request.ExpiryWarningDays,
                request.DefaultLowStockThreshold,
                request.AutoDeductOnCompleted,
                request.MaxCostRecalcDays
            );

            var repo = _unitOfWork.Repository<InventorySettings>();
            var actorId = _currentUserService.GetUserIdAsGuid();

            var settings = await repo.Query()
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                    cancellationToken
                );

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (settings == null)
                {
                    settings = InventorySettings.CreateDefault(actorId);
                    await repo.AddAsync(settings);
                }

                var domainResult = settings.Update(
                    request.ExpiryWarningDays,
                    request.DefaultLowStockThreshold,
                    request.AutoDeductOnCompleted!.Value,
                    request.MaxCostRecalcDays,
                    actorId
                );

                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "UpdateInventorySettings failed with error {ErrorCode}",
                        domainResult.ErrorCode
                    );
                    throw new BusinessException(
                        _messageService.GetMessage(
                            domainResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                        )
                    );
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveAsync(CacheKey.InventorySettings, cancellationToken);

                var response = new UpdateInventorySettingsResponse
                {
                    ExpiryWarningDays = settings.ExpiryWarningDays,
                    DefaultLowStockThreshold = settings.DefaultLowStockThreshold,
                    AutoDeductOnCompleted = settings.AutoDeductOnCompleted,
                    CostMethod = settings.CostMethod,
                    MaxCostRecalcDays = settings.MaxCostRecalcDays,
                    OpeningStockStatus = settings.OpeningStockStatus,
                    LockedAt = settings.LockedAt,
                };

                _logger.LogInformation("End handling UpdateInventorySettings");
                return Result<UpdateInventorySettingsResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
