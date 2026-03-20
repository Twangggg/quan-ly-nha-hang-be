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

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptHandler
        : IRequestHandler<CreateStockInReceiptCommand, Result<CreateStockInReceiptResponse>>
    {
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateStockInReceiptHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateStockInReceiptHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            ILogger<CreateStockInReceiptHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _logger = logger;
        }

        public async Task<Result<CreateStockInReceiptResponse>> Handle(
            CreateStockInReceiptCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling CreateStockInReceipt with {ItemCount} items",
                request.Items.Count
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var ingredientIds = request.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var receiptRepo = _unitOfWork.Repository<StockInReceipt>();
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            var ingredients = await ingredientRepo
                .Query()
                .Include(x => x.Conversions)
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            if (ingredients.Count != ingredientIds.Count)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                );
            }

            if (ingredients.Any(x => !x.IsActive))
            {
                throw new BusinessException(
                    _messageService.GetMessage(MessageKeys.Ingredient.Inactive)
                );
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);
            var receivedAt = NormalizeUtc(request.ReceivedAt) ?? DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var receiptCode = await GenerateReceiptCodeAsync(receivedAt, cancellationToken);
                var receipt = StockInReceipt.Create(receiptCode, receivedAt, request.Note, actorId);

                foreach (var item in request.Items)
                {
                    var ingredient = ingredientMap[item.IngredientId];
                    var baseQuantity = item.BaseUnit is null
                        ? item.Quantity
                        : ConvertToBase(ingredient, item.BaseUnit, item.Quantity);

                    var addItemResult = receipt.AddItem(
                        item.IngredientId,
                        baseQuantity,
                        item.BaseUnit ?? ingredient.BaseUnit,
                        item.UnitCost,
                        NormalizeUtc(item.ExpiryDate),
                        item.BatchCode,
                        actorId
                    );

                    if (!addItemResult.IsSuccess)
                    {
                        throw new BusinessException(
                            _messageService.GetMessage(
                                addItemResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                            )
                        );
                    }

                    var receiveResult = ingredient.ReceiveStock(
                        baseQuantity,
                        item.UnitCost,
                        actorId
                    );

                    if (!receiveResult.IsSuccess)
                    {
                        throw new BusinessException(
                            _messageService.GetMessage(
                                receiveResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                            )
                        );
                    }

                    await transactionRepo.AddAsync(
                        InventoryTransaction.CreateStockIn(
                            ingredient.IngredientId,
                            baseQuantity,
                            item.UnitCost,
                            ingredient.CurrentStock,
                            receiptCode,
                            actorId
                        )
                    );
                }

                await receiptRepo.AddAsync(receipt);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _inventoryAvailabilitySyncService.SyncAfterStockChangeAsync(
                    ingredientIds,
                    cancellationToken
                );
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling CreateStockInReceipt with ReceiptCode={ReceiptCode}",
                    receipt.ReceiptCode
                );

                return Result<CreateStockInReceiptResponse>.Success(
                    new CreateStockInReceiptResponse
                    {
                        StockInReceiptId = receipt.StockInReceiptId,
                        ReceiptCode = receipt.ReceiptCode,
                        ReceivedAt = receipt.ReceivedAt,
                        TotalLines = receipt.TotalLines,
                        TotalAmount = receipt.TotalAmount,
                        CreatedAt = receipt.CreatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "CreateStockInReceipt transaction rolled back");
                throw;
            }
        }

        private async Task<string> GenerateReceiptCodeAsync(
            DateTime receivedAt,
            CancellationToken cancellationToken
        )
        {
            var datePart = receivedAt.ToString("yyyyMMdd");
            var prefix = $"NK-{datePart}-";

            var lastReceipt = await _unitOfWork
                .Repository<StockInReceipt>()
                .Query()
                .Where(x => x.ReceiptCode.StartsWith(prefix))
                .OrderByDescending(x => x.ReceiptCode)
                .FirstOrDefaultAsync(cancellationToken);

            var sequenceNumber = 1;
            if (lastReceipt is not null)
            {
                var parts = lastReceipt.ReceiptCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }
            return $"{prefix}{sequenceNumber:D4}";
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
        }

        private static decimal ConvertToBase(
            Ingredient ingredient,
            string fromUnit,
            decimal quantity
        )
        {
            if (string.Equals(fromUnit, ingredient.BaseUnit, StringComparison.OrdinalIgnoreCase))
            {
                return quantity;
            }

            var conversion = ingredient.Conversions.FirstOrDefault(x =>
                string.Equals(x.FromUnit, fromUnit, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ToUnit, ingredient.BaseUnit, StringComparison.OrdinalIgnoreCase)
            );

            if (conversion == null)
            {
                throw new BusinessException(
                    $"Missing conversion from {fromUnit} to {ingredient.BaseUnit} for {ingredient.Name}"
                );
            }

            return Math.Round(quantity * conversion.Factor, 3, MidpointRounding.AwayFromZero);
        }
    }
}
