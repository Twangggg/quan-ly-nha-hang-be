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
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.ReverseStockOutReceipt
{
    public class ReverseStockOutReceiptHandler
        : IRequestHandler<ReverseStockOutReceiptCommand, Result<ReverseStockOutReceiptResponse>>
    {
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReverseStockOutReceiptHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public ReverseStockOutReceiptHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            ILogger<ReverseStockOutReceiptHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _logger = logger;
        }

        public async Task<Result<ReverseStockOutReceiptResponse>> Handle(
            ReverseStockOutReceiptCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling ReverseStockOutReceipt for StockOutReceiptId={StockOutReceiptId}",
                request.StockOutReceiptId
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var receiptRepo = _unitOfWork.Repository<StockOutReceipt>();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            var receipt = await receiptRepo
                .Query()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.StockOutReceiptId == request.StockOutReceiptId,
                    cancellationToken
                );

            if (receipt is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.StockOutReceipt.ReceiptNotFound)
                );
            }

            var ingredientIds = receipt.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredients = await ingredientRepo
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in receipt.Items)
                {
                    var ingredient = ingredientMap[item.IngredientId];
                    var reverseResult = ingredient.ReverseReducedStock(item.Quantity, actorId);

                    if (!reverseResult.IsSuccess)
                    {
                        throw new BusinessException(
                            _messageService.GetMessage(
                                reverseResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                            )
                        );
                    }

                    await transactionRepo.AddAsync(
                        InventoryTransaction.CreateStockOutReverse(
                            ingredient.IngredientId,
                            item.Quantity,
                            item.UnitPrice,
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
                    "End handling ReverseStockOutReceipt for ReceiptCode={ReceiptCode}",
                    receipt.ReceiptCode
                );

                return Result<ReverseStockOutReceiptResponse>.Success(
                    new ReverseStockOutReceiptResponse
                    {
                        StockOutReceiptId = receipt.StockOutReceiptId,
                        ReceiptCode = receipt.ReceiptCode,
                        ReversedAt = receipt.DeletedAt ?? DateTime.UtcNow,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "ReverseStockOutReceipt transaction rolled back");
                throw;
            }
        }
    }
}
