using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
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
        private readonly IReceiptCodeGenerator _receiptCodeGenerator;

        public ImportOpeningStockHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IReceiptCodeGenerator receiptCodeGenerator,
            ILogger<ImportOpeningStockHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _receiptCodeGenerator = receiptCodeGenerator;
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

            if (
                settings is not null
                && !settings.IsOpeningStockImportAllowedAt(DateTime.UtcNow)
            )
            {
                var nextAllowedAt = settings.GetNextOpeningStockImportAllowedAt();
                _logger.LogWarning(
                    "ImportOpeningStock rejected because cooldown has not elapsed. Next allowed at {NextAllowedAt}",
                    nextAllowedAt
                );
                throw new BusinessException(
                    nextAllowedAt.HasValue
                        ? $"{_messageService.GetMessage(MessageKeys.OpeningStock.ImportCooldownNotElapsed)} {nextAllowedAt.Value:dd/MM/yyyy HH:mm}."
                        : _messageService.GetMessage(MessageKeys.OpeningStock.ImportCooldownNotElapsed)
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
            var stockInReceiptRepo = _unitOfWork.Repository<StockInReceipt>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (settings == null)
                {
                    settings = InventorySettings.CreateDefault(actorId);
                    await settingsRepo.AddAsync(settings);
                }

                var transactionCount = 0;
                StockInReceipt? openingStockReceipt = null;

                if (settings is null)
                {
                    settings = InventorySettings.CreateDefault(actorId);
                    await settingsRepo.AddAsync(settings);
                }

                var openingStockItems = request.Items.Where(x => x.Quantity > 0).ToList();
                if (openingStockItems.Count > 0)
                {
                    var receiptTimestamp = DateTime.UtcNow;
                    var receiptCode = await _receiptCodeGenerator.GenerateStockInReceiptCodeAsync(
                        receiptTimestamp,
                        cancellationToken
                    );
                    openingStockReceipt = StockInReceipt.Create(
                        receiptCode,
                        receiptTimestamp,
                        "Opening stock import",
                        actorId
                    );
                    settings.MarkOpeningStockImported(receiptTimestamp, actorId);
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
                        var addReceiptItemResult = openingStockReceipt!.AddItem(
                            ingredient.IngredientId,
                            item.Quantity,
                            ingredient.BaseUnit,
                            item.CostPrice,
                            null,
                            null,
                            actorId
                        );

                        if (!addReceiptItemResult.IsSuccess)
                        {
                            _logger.LogWarning(
                                "ImportOpeningStock failed to create stock-in receipt item for IngredientId={IngredientId} with error {ErrorCode}",
                                item.IngredientId,
                                addReceiptItemResult.ErrorCode
                            );
                            throw new BusinessException(
                                _messageService.GetMessage(
                                    addReceiptItemResult.ErrorCode
                                        ?? MessageKeys.Common.ValidationFailed
                                )
                            );
                        }

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

                if (openingStockReceipt is not null)
                {
                    await stockInReceiptRepo.AddAsync(openingStockReceipt);
                }

                settings.CompleteOpeningStock(actorId);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _cacheService.RemoveAsync(CacheKey.InventorySettings, cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveAsync(CacheKey.InventorySettings, cancellationToken);
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                var response = new ImportOpeningStockResponse
                {
                    UpdatedCount = request.Items.Count,
                    TransactionCount = transactionCount,
                    UpdatedAt = DateTime.UtcNow,
                    LockedAt = settings.LockedAt,
                    LastOpeningStockImportedAt = settings.LastOpeningStockImportedAt,
                    NextOpeningStockImportAllowedAt = settings.GetNextOpeningStockImportAllowedAt(),
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
