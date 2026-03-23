using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs
{
    public class RecalculateCogsHandler
        : IRequestHandler<RecalculateCogsCommand, Result<RecalculateCogsResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<RecalculateCogsHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly InventoryCostService _inventoryCostService;
        private readonly IUnitOfWork _unitOfWork;

        public RecalculateCogsHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            InventoryCostService inventoryCostService,
            ILogger<RecalculateCogsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _inventoryCostService = inventoryCostService;
            _logger = logger;
        }

        public async Task<Result<RecalculateCogsResponse>> Handle(
            RecalculateCogsCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling RecalculateCogs from {FromDate} to {ToDate} for IngredientId={IngredientId}",
                request.FromDate,
                request.ToDate,
                request.IngredientId
            );

            var actorId = _currentUserService.GetUserIdAsGuid();
            var fromInclusive = ToUtcStart(request.FromDate);
            var toExclusive = ToUtcExclusiveEnd(request.ToDate);

            var settings =
                await _unitOfWork
                    .Repository<InventorySettings>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                        cancellationToken
                    )
                ?? InventorySettings.CreateDefault();

            var selectedDayCount = request.ToDate.DayNumber - request.FromDate.DayNumber + 1;
            if (selectedDayCount > settings.MaxCostRecalcDays)
            {
                throw new BusinessException(
                    _messageService.GetMessage(MessageKeys.InventorySettings.MaxCostRecalcDaysRange)
                );
            }

            IQueryable<Ingredient> ingredientQuery = _unitOfWork.Repository<Ingredient>().Query();
            if (request.IngredientId.HasValue)
            {
                ingredientQuery = ingredientQuery.Where(x =>
                    x.IngredientId == request.IngredientId.Value
                );
            }

            var ingredientIds = await ingredientQuery
                .Select(x => x.IngredientId)
                .ToListAsync(cancellationToken);

            if (request.IngredientId.HasValue && ingredientIds.Count == 0)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                );
            }

            var stockOutItems = await _unitOfWork
                .Repository<StockOutReceiptItem>()
                .Query()
                .Include(x => x.StockOutReceipt)
                .Include(x => x.LotAllocations)
                .Where(x =>
                    ingredientIds.Contains(x.IngredientId)
                    && x.StockOutReceipt.DeletedAt == null
                    && x.StockOutReceipt.StockOutDate < toExclusive
                )
                .ToListAsync(cancellationToken);

            var inRangeStockOutItems = stockOutItems
                .Where(x => x.StockOutReceipt.StockOutDate >= fromInclusive)
                .ToList();

            if (inRangeStockOutItems.Count == 0)
            {
                _logger.LogInformation(
                    "End handling RecalculateCogs with no stock-out items in range for IngredientId={IngredientId}",
                    request.IngredientId
                );

                return Result<RecalculateCogsResponse>.Success(
                    new RecalculateCogsResponse
                    {
                        FromDate = request.FromDate,
                        ToDate = request.ToDate,
                        ProcessedIngredients = ingredientIds.Count,
                        UpdatedReceipts = 0,
                        UpdatedItems = 0,
                        TotalAdjustmentAmount = 0,
                        Message = _messageService.GetMessage(MessageKeys.InventoryCogs.Completed),
                    }
                );
            }

            var stockInItems = await _unitOfWork
                .Repository<StockInReceiptItem>()
                .Query()
                .AsNoTracking()
                .Include(x => x.StockInReceipt)
                .Where(x =>
                    ingredientIds.Contains(x.IngredientId)
                    && x.StockInReceipt.DeletedAt == null
                    && x.StockInReceipt.ReceivedAt < toExclusive
                )
                .ToListAsync(cancellationToken);

            var openingTransactions = await _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Where(x =>
                    ingredientIds.Contains(x.IngredientId)
                    && x.TransactionType == InventoryTransactionType.OpeningStock
                    && x.OccurredAt < toExclusive
                )
                .ToListAsync(cancellationToken);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var recalculation = _inventoryCostService.Recalculate(
                    openingTransactions,
                    stockInItems,
                    stockOutItems,
                    fromInclusive,
                    toExclusive
                );

                var calculatedAt = DateTime.UtcNow;
                foreach (var update in recalculation.Updates)
                {
                    update.StockOutItem.RestateCost(
                        update.UnitCost,
                        InventoryCostCalculationSource.PeriodRecalculation,
                        calculatedAt,
                        actorId
                    );

                    foreach (var allocation in update.StockOutItem.LotAllocations)
                    {
                        allocation.UpdateCost(update.UnitCost, actorId);
                    }
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                _logger.LogInformation(
                    "End handling RecalculateCogs with {UpdatedItems} updated items across {UpdatedReceipts} receipts",
                    recalculation.UpdatedItemCount,
                    recalculation.UpdatedReceiptCount
                );

                return Result<RecalculateCogsResponse>.Success(
                    new RecalculateCogsResponse
                    {
                        FromDate = request.FromDate,
                        ToDate = request.ToDate,
                        ProcessedIngredients = recalculation.UpdatedIngredientCount,
                        UpdatedReceipts = recalculation.UpdatedReceiptCount,
                        UpdatedItems = recalculation.UpdatedItemCount,
                        TotalAdjustmentAmount = recalculation.TotalDelta,
                        Message = _messageService.GetMessage(MessageKeys.InventoryCogs.Completed),
                    }
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "RecalculateCogs transaction rolled back");
                throw;
            }
        }

        private static DateTime ToUtcStart(DateOnly value)
        {
            return DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }

        private static DateTime ToUtcExclusiveEnd(DateOnly value)
        {
            return DateTime.SpecifyKind(
                value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc
            );
        }
    }
}
