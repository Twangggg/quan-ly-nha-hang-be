using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt
{
    public class CreateStockOutReceiptHandler
        : IRequestHandler<CreateStockOutReceiptCommand, Result<CreateStockOutReceiptResponse>>
    {
        private readonly IInventoryAvailabilitySyncService _inventoryAvailabilitySyncService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateStockOutReceiptHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateStockOutReceiptHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IInventoryAvailabilitySyncService inventoryAvailabilitySyncService,
            ILogger<CreateStockOutReceiptHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _inventoryAvailabilitySyncService = inventoryAvailabilitySyncService;
            _logger = logger;
        }

        public async Task<Result<CreateStockOutReceiptResponse>> Handle(
            CreateStockOutReceiptCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling CreateStockOutReceipt with {ItemCount} items",
                request.Items.Count
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var ingredientIds = request.Items.Select(x => x.IngredientId).Distinct().ToList();
            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var receiptRepo = _unitOfWork.Repository<StockOutReceipt>();
            var transactionRepo = _unitOfWork.Repository<InventoryTransaction>();

            var ingredients = await ingredientRepo
                .Query()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            if (ingredients.Count != ingredientIds.Count)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.Common.NotFound)
                );
            }

            if (ingredients.Any(x => !x.IsActive))
            {
                throw new BusinessException(
                    _messageService.GetMessage(MessageKeys.Ingredient.Inactive)
                );
            }

            var ingredientMap = ingredients.ToDictionary(x => x.IngredientId);
            var stockOutDate = NormalizeUtc(request.StockOutDate) ?? DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var receiptCode = await GenerateReceiptCodeAsync(stockOutDate, cancellationToken);
                var receipt = StockOutReceipt.Create(
                    receiptCode,
                    stockOutDate,
                    request.Note,
                    actorId
                );

                foreach (var item in request.Items)
                {
                    var addItemResult = receipt.AddItem(
                        item.IngredientId,
                        item.Quantity,
                        item.UnitPrice,
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

                    var ingredient = ingredientMap[item.IngredientId];
                    var reduceResult = ingredient.ReduceStock(item.Quantity, actorId);

                    if (!reduceResult.IsSuccess)
                    {
                        throw new BusinessException(
                            _messageService.GetMessage(
                                reduceResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                            )
                        );
                    }

                    await transactionRepo.AddAsync(
                        InventoryTransaction.CreateStockOut(
                            ingredient.IngredientId,
                            item.Quantity,
                            item.UnitPrice,
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

                _logger.LogInformation(
                    "End handling CreateStockOutReceipt with ReceiptCode={ReceiptCode}",
                    receipt.ReceiptCode
                );

                return Result<CreateStockOutReceiptResponse>.Success(
                    new CreateStockOutReceiptResponse
                    {
                        StockOutReceiptId = receipt.StockOutReceiptId,
                        ReceiptCode = receipt.ReceiptCode,
                        StockOutDate = receipt.StockOutDate,
                        TotalAmount = receipt.TotalAmount,
                        CreatedAt = receipt.CreatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "CreateStockOutReceipt transaction rolled back");
                throw;
            }
        }

        private async Task<string> GenerateReceiptCodeAsync(
            DateTime stockOutDate,
            CancellationToken cancellationToken
        )
        {
            var datePart = stockOutDate.ToString("yyyyMMdd");
            var prefix = $"XK-{datePart}-";

            var lastReceipt = await _unitOfWork
                .Repository<StockOutReceipt>()
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
    }
}
