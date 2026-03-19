using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck
{
    public class ProcessInventoryCheckHandler
        : IRequestHandler<ProcessInventoryCheckCommand, Result<ProcessInventoryCheckResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ILogger<ProcessInventoryCheckHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IReceiptCodeGenerator _receiptCodeGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessInventoryCheckHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            IReceiptCodeGenerator receiptCodeGenerator,
            ILogger<ProcessInventoryCheckHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _receiptCodeGenerator = receiptCodeGenerator;
            _logger = logger;
        }

        public async Task<Result<ProcessInventoryCheckResponse>> Handle(
            ProcessInventoryCheckCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling ProcessInventoryCheck for InventoryCheckId={InventoryCheckId}",
                request.InventoryCheckId
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var inventoryCheck = await _unitOfWork
                .Repository<InventoryCheck>()
                .Query()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.InventoryCheckId == request.InventoryCheckId,
                    cancellationToken
                );

            if (inventoryCheck is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.InventoryCheck.CheckNotFound)
                );
            }

            var processableResult = inventoryCheck.EnsureProcessable();
            if (!processableResult.IsSuccess)
            {
                throw new BusinessException(
                    _messageService.GetMessage(
                        processableResult.ErrorCode ?? MessageKeys.InventoryCheck.InvalidStatus
                    )
                );
            }

            var ingredientIds = inventoryCheck.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredients = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId) && x.IsActive)
                .ToListAsync(cancellationToken);

            if (ingredients.Count != ingredientIds.Count)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                );
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);
            var receiptTimestamp = DateTime.UtcNow;
            var stockInItems = inventoryCheck.Items.Where(x => x.DifferenceQuantity > 0).ToList();
            var stockOutItems = inventoryCheck.Items.Where(x => x.DifferenceQuantity < 0).ToList();

            StockInReceipt? stockInReceipt = null;
            StockOutReceipt? stockOutReceipt = null;
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (stockInItems.Count > 0)
                {
                    stockInReceipt = StockInReceipt.CreateInventoryAdjustment(
                        await _receiptCodeGenerator.GenerateStockInReceiptCodeAsync(receiptTimestamp, cancellationToken),
                        receiptTimestamp,
                        $"Inventory adjustment from check {inventoryCheck.InventoryCheckId}",
                        actorId
                    );

                    foreach (var item in stockInItems)
                    {
                        var ingredient = ingredientMap[item.IngredientId];
                        var quantity = item.DifferenceQuantity;

                        var addReceiptItemResult = stockInReceipt.AddItem(
                            item.IngredientId,
                            quantity,
                            ingredient.BaseUnit,
                            ingredient.CostPrice,
                            null,
                            null,
                            actorId
                        );

                        if (!addReceiptItemResult.IsSuccess)
                        {
                            throw new BusinessException(
                                _messageService.GetMessage(
                                    addReceiptItemResult.ErrorCode
                                        ?? MessageKeys.Common.ValidationFailed
                                )
                            );
                        }

                        var applyInventoryCheckResult = ingredient.ApplyInventoryCheck(
                            item.PhysicalQuantity,
                            actorId
                        );

                        if (!applyInventoryCheckResult.IsSuccess)
                        {
                            throw new BusinessException(
                                _messageService.GetMessage(
                                    applyInventoryCheckResult.ErrorCode
                                        ?? MessageKeys.Common.ValidationFailed
                                )
                            );
                        }

                        await transactionRepo.AddAsync(
                            InventoryTransaction.CreateInventoryCheck(
                                ingredient.IngredientId,
                                quantity,
                                ingredient.CostPrice,
                                ingredient.CurrentStock,
                                inventoryCheck.InventoryCheckId.ToString(),
                                actorId
                            )
                        );
                    }

                    await _unitOfWork.Repository<StockInReceipt>().AddAsync(stockInReceipt);
                }

                if (stockOutItems.Count > 0)
                {
                    stockOutReceipt = StockOutReceipt.CreateInventoryAdjustment(
                        await _receiptCodeGenerator.GenerateStockOutReceiptCodeAsync(receiptTimestamp, cancellationToken),
                        receiptTimestamp,
                        $"Inventory adjustment from check {inventoryCheck.InventoryCheckId}",
                        actorId
                    );

                    foreach (var item in stockOutItems)
                    {
                        var ingredient = ingredientMap[item.IngredientId];
                        var quantity = Math.Abs(item.DifferenceQuantity);

                        var addReceiptItemResult = stockOutReceipt.AddItem(
                            item.IngredientId,
                            quantity,
                            ingredient.CostPrice,
                            actorId
                        );

                        if (!addReceiptItemResult.IsSuccess)
                        {
                            throw new BusinessException(
                                _messageService.GetMessage(
                                    addReceiptItemResult.ErrorCode
                                        ?? MessageKeys.Common.ValidationFailed
                                )
                            );
                        }

                        var applyInventoryCheckResult = ingredient.ApplyInventoryCheck(
                            item.PhysicalQuantity,
                            actorId
                        );

                        if (!applyInventoryCheckResult.IsSuccess)
                        {
                            throw new BusinessException(
                                _messageService.GetMessage(
                                    applyInventoryCheckResult.ErrorCode
                                        ?? MessageKeys.Common.ValidationFailed
                                )
                            );
                        }

                        await transactionRepo.AddAsync(
                            InventoryTransaction.CreateInventoryCheck(
                                ingredient.IngredientId,
                                -quantity,
                                ingredient.CostPrice,
                                ingredient.CurrentStock,
                                inventoryCheck.InventoryCheckId.ToString(),
                                actorId
                            )
                        );
                    }

                    await _unitOfWork.Repository<StockOutReceipt>().AddAsync(stockOutReceipt);
                }

                var markProcessedResult = inventoryCheck.MarkProcessed(actorId);
                if (!markProcessedResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            markProcessedResult.ErrorCode ?? MessageKeys.InventoryCheck.InvalidStatus
                        )
                    );
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                if (ingredientIds.Count > 0)
                {
                    await _inventoryAvailabilitySyncService.SyncAfterStockChangeAsync(
                        ingredientIds,
                        cancellationToken
                    );
                }

                _logger.LogInformation(
                    "End handling ProcessInventoryCheck for InventoryCheckId={InventoryCheckId}",
                    inventoryCheck.InventoryCheckId
                );

                return Result<ProcessInventoryCheckResponse>.Success(
                    new ProcessInventoryCheckResponse
                    {
                        InventoryCheckId = inventoryCheck.InventoryCheckId,
                        Status = InventoryCheckStatus.Processed,
                        ProcessedAt = inventoryCheck.ProcessedAt,
                        StockInReceiptId = stockInReceipt?.StockInReceiptId,
                        StockInReceiptCode = stockInReceipt?.ReceiptCode,
                        StockOutReceiptId = stockOutReceipt?.StockOutReceiptId,
                        StockOutReceiptCode = stockOutReceipt?.ReceiptCode,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "ProcessInventoryCheck transaction rolled back");
                throw;
            }
        }
    }
}
