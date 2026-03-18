using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public class GetInventoryReportHandler
        : IRequestHandler<GetInventoryReportQuery, Result<IReadOnlyList<GetInventoryReportResponse>>>
    {
        private readonly ILogger<GetInventoryReportHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public GetInventoryReportHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetInventoryReportHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<GetInventoryReportResponse>>> Handle(
            GetInventoryReportQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryReport from {FromDate} to {ToDate} for IngredientId={IngredientId}",
                request.FromDate,
                request.ToDate,
                request.IngredientId
            );

            var from = ToUtcStart(request.FromDate);
            var toExclusive = ToUtcExclusiveEnd(request.ToDate);

            var ingredientsQuery = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();
            if (request.IngredientId.HasValue)
            {
                ingredientsQuery = ingredientsQuery.Where(
                    x => x.IngredientId == request.IngredientId.Value
                );
            }

            var ingredients = await ingredientsQuery
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            var ingredientIds = ingredients.Select(x => x.IngredientId).ToList();

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
                        IngredientName = ingredient.Name,
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

            _logger.LogInformation(
                "End handling GetInventoryReport with {Count} items",
                responses.Count
            );

            return Result<IReadOnlyList<GetInventoryReportResponse>>.Success(responses);
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
