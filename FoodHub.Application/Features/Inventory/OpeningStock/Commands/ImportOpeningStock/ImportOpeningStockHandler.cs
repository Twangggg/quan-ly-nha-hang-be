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

namespace FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock
{
    public class ImportOpeningStockHandler
        : IRequestHandler<ImportOpeningStockCommand, Result<ImportOpeningStockResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ImportOpeningStockHandler> _logger;

        public ImportOpeningStockHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<ImportOpeningStockHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<ImportOpeningStockResponse>> Handle(
            ImportOpeningStockCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling ImportOpeningStock with {ItemCount} items and ConfirmOverwrite={ConfirmOverwrite}",
                request.Items.Count,
                request.ConfirmOverwrite
            );

            var ingredientIds = request.Items.Select(x => x.IngredientId).Distinct().ToList();
            var actorId = _currentUserService.GetUserIdAsGuid();
            var settingsRepo = _unitOfWork.Repository<InventorySettings>();

            var ingredients = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId) && x.IsActive)
                .ToListAsync(cancellationToken);

            var settings = await settingsRepo
                .Query()
                .FirstOrDefaultAsync(
                    x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                    cancellationToken
                );

            if (
                settings is not null
                && (
                    settings.OpeningStockStatus == Domain.Enums.OpeningStockStatus.Completed
                    || settings.LockedAt.HasValue
                )
            )
            {
                _logger.LogWarning("ImportOpeningStock rejected because opening stock is locked");
                throw new BusinessException(
                    _messageService.GetMessage(MessageKeys.OpeningStock.AlreadyLocked)
                );
            }

            if (ingredients.Count != ingredientIds.Count)
            {
                _logger.LogWarning(
                    "ImportOpeningStock failed because one or more ingredients were not found"
                );
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.OpeningStock.IngredientNotFound)
                );
            }

            if (!request.ConfirmOverwrite && ingredients.Any(x => x.CurrentStock > 0))
            {
                _logger.LogWarning(
                    "ImportOpeningStock requires overwrite confirmation for existing stock"
                );
                throw new BusinessException(
                    _messageService.GetMessage(MessageKeys.OpeningStock.ConfirmOverwrite)
                );
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (settings == null)
                {
                    settings = InventorySettings.CreateDefault(actorId);
                    await settingsRepo.AddAsync(settings);
                }

                var transactionCount = 0;

                if (settings is null)
                {
                    settings = InventorySettings.CreateDefault(actorId);
                    await settingsRepo.AddAsync(settings);
                }

                foreach (var item in request.Items)
                {
                    var ingredient = ingredientMap[item.IngredientId];
                    var domainResult = ingredient.SetOpeningStock(
                        item.Quantity,
                        item.CostPrice,
                        actorId
                    );

                    if (!domainResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "ImportOpeningStock failed for IngredientId={IngredientId} with error {ErrorCode}",
                            item.IngredientId,
                            domainResult.ErrorCode
                        );
                        throw new BusinessException(
                            _messageService.GetMessage(
                                domainResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                            )
                        );
                    }

                    if (item.Quantity > 0)
                    {
                        await transactionRepo.AddAsync(
                            InventoryTransaction.CreateOpeningStock(
                                ingredient.IngredientId,
                                item.Quantity,
                                item.CostPrice,
                                ingredient.CurrentStock,
                                null,
                                actorId
                            )
                        );
                        transactionCount++;
                    }
                }

                settings.CompleteOpeningStock(actorId);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveAsync(CacheKey.InventorySettings, cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveAsync(CacheKey.InventorySettings, cancellationToken);

                var response = new ImportOpeningStockResponse
                {
                    UpdatedCount = request.Items.Count,
                    TransactionCount = transactionCount,
                    UpdatedAt = DateTime.UtcNow,
                };

                _logger.LogInformation(
                    "End handling ImportOpeningStock with UpdatedCount={UpdatedCount} and TransactionCount={TransactionCount}",
                    response.UpdatedCount,
                    response.TransactionCount
                );

                return Result<ImportOpeningStockResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "ImportOpeningStock transaction rolled back");
                throw;
            }
        }
    }
}
