using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public class GetInventoryReportHandler
        : IRequestHandler<GetInventoryReportQuery, Result<PagedResult<GetInventoryReportResponse>>>
    {
        private readonly ILogger<GetInventoryReportHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetInventoryReportHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryReportHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryReportResponse>>> Handle(
            GetInventoryReportQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryReport from {FromDate} to {ToDate} for IngredientId={IngredientId}, Page={Page}, Size={Size}",
                request.FromDate,
                request.ToDate,
                request.IngredientId,
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryReportList, request);
            var cached = await _cacheService.GetAsync<PagedResult<GetInventoryReportResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryReport with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );
                return Result<PagedResult<GetInventoryReportResponse>>.Success(cached);
            }

            var from = ToUtcStart(request.FromDate);
            var toExclusive = ToUtcExclusiveEnd(request.ToDate);

            var ingredientsQuery = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();
            if (request.IngredientId.HasValue)
            {
                ingredientsQuery = ingredientsQuery.Where(
                    x => x.IngredientId == request.IngredientId.Value
                );
            }

            var ingredientIds = await ingredientsQuery
                .OrderBy(x => x.Name)
                .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .Select(x => x.IngredientId)
                .ToListAsync(cancellationToken);

            if (ingredientIds.Count == 0)
            {
                var emptyResult = new PagedResult<GetInventoryReportResponse>(
                    Array.Empty<GetInventoryReportResponse>(),
                    request.Pagination,
                    0
                );
                return Result<PagedResult<GetInventoryReportResponse>>.Success(emptyResult);
            }

            var priorTransactions = await _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Where(x => ingredientIds.Contains(x.IngredientId) && x.OccurredAt < from)
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            var stockInItems = await _unitOfWork
                .Repository<StockInReceiptItem>()
                .Query()
                .AsNoTracking()
                .Include(x => x.StockInReceipt)
                .Where(
                    x =>
                        ingredientIds.Contains(x.IngredientId)
                        && x.DeletedAt == null
                        && x.StockInReceipt.DeletedAt == null
                        && x.StockInReceipt.ReceivedAt >= from
                        && x.StockInReceipt.ReceivedAt < toExclusive
                )
                .ToListAsync(cancellationToken);

            var stockOutItems = await _unitOfWork
                .Repository<StockOutReceiptItem>()
                .Query()
                .AsNoTracking()
                .Include(x => x.StockOutReceipt)
                .Where(
                    x =>
                        ingredientIds.Contains(x.IngredientId)
                        && x.DeletedAt == null
                        && x.StockOutReceipt.DeletedAt == null
                        && x.StockOutReceipt.StockOutDate >= from
                        && x.StockOutReceipt.StockOutDate < toExclusive
                )
                .ToListAsync(cancellationToken);

            var saleDeductions = await _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Where(
                    x =>
                        ingredientIds.Contains(x.IngredientId)
                        && x.TransactionType == InventoryTransactionType.SaleDeduction
                        && x.OccurredAt >= from
                        && x.OccurredAt < toExclusive
                )
                .ToListAsync(cancellationToken);

            var totalCount = await ingredientsQuery.CountAsync(cancellationToken);

            var ingredients = await _unitOfWork
                .Repository<Ingredient>()
                .Query()
                .AsNoTracking()
                .Where(x => ingredientIds.Contains(x.IngredientId))
                .ToListAsync(cancellationToken);

            var openingMap = priorTransactions
                .GroupBy(x => x.IngredientId)
                .ToDictionary(x => x.Key, x => x.Last().BalanceAfter);

            var stockInMap = stockInItems
                .GroupBy(x => x.IngredientId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));

            var stockOutMap = stockOutItems
                .GroupBy(x => x.IngredientId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));

            var saleDeductionMap = saleDeductions
                .GroupBy(x => x.IngredientId)
                .ToDictionary(x => x.Key, x => x.Sum(y => Math.Abs(y.Quantity)));

            var responses = ingredients
                .Select(ingredient =>
                {
                    var openingStock = openingMap.GetValueOrDefault(ingredient.IngredientId);
                    var totalStockIn = stockInMap.GetValueOrDefault(ingredient.IngredientId);
                    var totalStockOut = stockOutMap.GetValueOrDefault(ingredient.IngredientId);
                    var totalSaleDeduction = saleDeductionMap.GetValueOrDefault(
                        ingredient.IngredientId
                    );
                    var totalOutbound = totalStockOut + totalSaleDeduction;
                    var closingStock = openingStock + totalStockIn - totalOutbound;

                    return new GetInventoryReportResponse
                    {
                        IngredientId = ingredient.IngredientId,
                        IngredientCode = ingredient.Code,
                        IngredientName = ingredient.Name,
                        Unit = ingredient.BaseUnit,
                        OpeningStock = openingStock,
                        TotalStockIn = totalStockIn,
                        TotalStockOut = totalStockOut,
                        TotalSaleDeduction = totalSaleDeduction,
                        TotalOutbound = totalOutbound,
                        ClosingStock = closingStock,
                        AverageUnitCost = ingredient.CostPrice,
                        ClosingStockValue = closingStock * ingredient.CostPrice,
                    };
                })
                .ToList();

            var pagedResult = new PagedResult<GetInventoryReportResponse>(
                responses,
                request.Pagination,
                totalCount
            );

            _logger.LogInformation(
                "End handling GetInventoryReport with {Count} items out of {TotalCount}",
                responses.Count,
                totalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<PagedResult<GetInventoryReportResponse>>.Success(pagedResult);
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
