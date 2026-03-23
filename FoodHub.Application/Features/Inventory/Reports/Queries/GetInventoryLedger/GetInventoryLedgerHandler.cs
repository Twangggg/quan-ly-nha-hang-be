using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public class GetInventoryLedgerHandler
        : IRequestHandler<GetInventoryLedgerQuery, Result<PagedResult<GetInventoryLedgerResponse>>>
    {
        private readonly ILogger<GetInventoryLedgerHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetInventoryLedgerHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryLedgerHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryLedgerResponse>>> Handle(
            GetInventoryLedgerQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryLedger for IngredientId={IngredientId} from {FromDate} to {ToDate}",
                request.IngredientId,
                request.FromDate,
                request.ToDate
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryLedgerList, request);
            var cached = await _cacheService.GetAsync<PagedResult<GetInventoryLedgerResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryLedger with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );
                return Result<PagedResult<GetInventoryLedgerResponse>>.Success(cached);
            }

            var from = ToUtcStart(request.FromDate);
            var toExclusive = ToUtcExclusiveEnd(request.ToDate);

            var query = _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Ingredient)
                .Where(
                    x =>
                        x.OccurredAt >= from
                        && x.OccurredAt < toExclusive
                );

            if (request.IngredientId.HasValue)
            {
                query = query.Where(x => x.IngredientId == request.IngredientId.Value);
            }

            if (request.TransactionType.HasValue)
            {
                query = query.Where(x => x.TransactionType == request.TransactionType.Value);
            }

            var orderedQuery = query
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.CreatedAt);

            var pagedResult = await orderedQuery
                .Select(x => new GetInventoryLedgerResponse
                {
                    OccurredAt = x.OccurredAt,
                    TransactionType = x.TransactionType,
                    ReferenceNo = x.Reference,
                    QuantityDelta = x.Quantity,
                    BalanceAfter = x.BalanceAfter,
                    IngredientName = x.Ingredient.Name,
                    IngredientId = x.IngredientId,
                })
                .ToPagedResultAsync(
                    new PaginationParams { PageNumber = 1, PageSize = 100 },
                    cancellationToken
                );

            _logger.LogInformation(
                "End handling GetInventoryLedger with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<PagedResult<GetInventoryLedgerResponse>>.Success(pagedResult);
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
