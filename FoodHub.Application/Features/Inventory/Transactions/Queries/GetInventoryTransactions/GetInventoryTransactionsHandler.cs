using System.Linq.Expressions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
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

namespace FoodHub.Application.Features.Inventory.Transactions.Queries.GetInventoryTransactions
{
    public class GetInventoryTransactionsHandler
        : IRequestHandler<
            GetInventoryTransactionsQuery,
            Result<PagedResult<GetInventoryTransactionsResponse>>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetInventoryTransactionsHandler> _logger;

        public GetInventoryTransactionsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryTransactionsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryTransactionsResponse>>> Handle(
            GetInventoryTransactionsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryTransactions with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryTransactionsList, request);
            var cached = await _cacheService.GetAsync<PagedResult<GetInventoryTransactionsResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryTransactions with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );
                return Result<PagedResult<GetInventoryTransactionsResponse>>.Success(cached);
            }

            IQueryable<InventoryTransaction> query = _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Ingredient);

            var searchableFields = new List<Expression<Func<InventoryTransaction, string?>>>
            {
                x => x.Ingredient.Name,
                x => x.Ingredient.Code,
                x => x.Reference,
            };

            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<InventoryTransaction, object?>>>
            {
                { "ingredientid", x => x.IngredientId },
                { "ingredientcode", x => x.Ingredient.Code },
                { "ingredientname", x => x.Ingredient.Name },
                { "transactiontype", x => x.TransactionType },
                { "occurredat", x => x.OccurredAt },
            };

            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<InventoryTransaction, object?>>>
            {
                { "occurredat", x => x.OccurredAt },
                { "createdat", x => x.CreatedAt },
                { "ingredientname", x => x.Ingredient.Name },
                { "transactiontype", x => x.TransactionType },
            };

            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, x => x.OccurredAt);

            var projection = query.Select(x => new GetInventoryTransactionsResponse
            {
                InventoryTransactionId = x.InventoryTransactionId,
                IngredientId = x.IngredientId,
                IngredientName = x.Ingredient.Name,
                IngredientCode = x.Ingredient.Code,
                TransactionType = x.TransactionType,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost,
                BalanceAfter = x.BalanceAfter,
                Reference = x.Reference,
                OccurredAt = x.OccurredAt,
            });

            var pagedResult = await projection.ToPagedResultAsync(
                request.Pagination,
                cancellationToken
            );

            _logger.LogInformation(
                "End handling GetInventoryTransactions with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<PagedResult<GetInventoryTransactionsResponse>>.Success(pagedResult);
        }
    }
}
