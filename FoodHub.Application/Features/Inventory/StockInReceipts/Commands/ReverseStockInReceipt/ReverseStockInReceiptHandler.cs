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
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt
{
    public class ReverseStockInReceiptHandler
        : IRequestHandler<ReverseStockInReceiptCommand, Result<ReverseStockInReceiptResponse>>
    {
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReverseStockInReceiptHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public ReverseStockInReceiptHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            ILogger<ReverseStockInReceiptHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _logger = logger;
        }

        public async Task<Result<ReverseStockInReceiptResponse>> Handle(
            ReverseStockInReceiptCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling ReverseStockInReceipt for StockInReceiptId={StockInReceiptId}",
                request.StockInReceiptId
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var receiptRepo = _unitOfWork.Repository<StockInReceipt>();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            var receipt = await receiptRepo
                .Query()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.StockInReceiptId == request.StockInReceiptId, cancellationToken);

            if (receipt is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.StockInReceipt.ReceiptNotFound)
                );
            }

            var ingredientIds = receipt.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredients = await ingredientRepo
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            if (ingredients.Count != ingredientIds.Count)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.StockInReceipt.ReceiptNotFound)
                );
            }

            var latestTransactions = await transactionRepo
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            var latestByIngredient = latestTransactions
                .GroupBy(x => x.IngredientId)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var item in receipt.Items)
            {
                if (
                    !latestByIngredient.TryGetValue(item.IngredientId, out var latestTransaction)
                    || latestTransaction.Reference != receipt.ReceiptCode
                    || latestTransaction.TransactionType != InventoryTransactionType.StockIn
                )
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            MessageKeys.StockInReceipt.ReverseNotLatestMovement
                        )
                    );
                }
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in receipt.Items)
                {
                    var ingredient = ingredientMap[item.IngredientId];
                    var reverseResult = ingredient.ReverseReceivedStock(
                        item.Quantity,
                        item.UnitCost,
                        actorId
                    );

                    if (!reverseResult.IsSuccess)
                    {
                        var messageKey =
                            reverseResult.ErrorCode == DomainErrors.Ingredient.InsufficientStock
                                ? MessageKeys.StockInReceipt.ReverseInsufficientStock
                                : reverseResult.ErrorCode ?? MessageKeys.Common.ValidationFailed;

                        throw new BusinessException(_messageService.GetMessage(messageKey));
                    }

                    await transactionRepo.AddAsync(
                        InventoryTransaction.CreateStockInReverse(
                            ingredient.IngredientId,
                            item.Quantity,
                            item.UnitCost,
                            ingredient.CurrentStock,
                            receipt.ReceiptCode,
                            actorId
                        )
                    );
                }

                var reverseReceiptResult = receipt.Reverse(actorId);
                if (!reverseReceiptResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            reverseReceiptResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                        )
                    );
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                await _inventoryAvailabilitySyncService.SyncAfterStockChangeAsync(
                    ingredientIds,
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling ReverseStockInReceipt for ReceiptCode={ReceiptCode}",
                    receipt.ReceiptCode
                );

                return Result<ReverseStockInReceiptResponse>.Success(
                    new ReverseStockInReceiptResponse
                    {
                        StockInReceiptId = receipt.StockInReceiptId,
                        ReceiptCode = receipt.ReceiptCode,
                        ReversedAt = receipt.DeletedAt ?? DateTime.UtcNow,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "ReverseStockInReceipt transaction rolled back");
                throw;
            }
        }
    }
}
