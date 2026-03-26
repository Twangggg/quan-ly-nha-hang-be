using System.Linq.Expressions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Lots.Queries.GetInventoryLots
{
    public class GetInventoryLotsHandler
        : IRequestHandler<GetInventoryLotsQuery, Result<PagedResult<GetInventoryLotsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetInventoryLotsHandler> _logger;

        public GetInventoryLotsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryLotsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryLotsResponse>>> Handle(
            GetInventoryLotsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryLots with PageNumber={PageNumber}, PageSize={PageSize}, Search={Search}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize,
                request.Pagination.Search
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryLotsList, request.Pagination);
            var cached = await _cacheService.GetAsync<PagedResult<GetInventoryLotsResponse>>(
                cacheKey,
                cancellationToken
            );

            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryLots with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );
                return Result<PagedResult<GetInventoryLotsResponse>>.Success(cached);
            }

            var today = DateTime.UtcNow.Date;
            var expiryWarningDays = await _unitOfWork
                .Repository<InventorySettings>()
                .Query()
                .AsNoTracking()
                .Where(x => x.SettingsKey == InventorySettings.DefaultSettingsKey)
                .Select(x => x.ExpiryWarningDays)
                .FirstOrDefaultAsync(cancellationToken);

            IQueryable<InventoryLot> query = _unitOfWork
                .Repository<InventoryLot>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Ingredient);

            var searchableFields = new List<Expression<Func<InventoryLot, string?>>>
            {
                x => x.LotCode,
                x => x.Ingredient.Name,
                x => x.Ingredient.Code,
            };

            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<InventoryLot, object?>>>
            {
                { "ingredientid", x => x.IngredientId },
                { "status", x => x.Status },
            };

            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            query = ApplySorting(query, request.Pagination.OrderBy);

            var pagedLots = await query.ToPagedResultAsync(request.Pagination, cancellationToken);

            var items = pagedLots.Items
                .Select(
                    x =>
                    {
                        var resolvedStatus = ResolveStatus(x, today, expiryWarningDays);
                        return new GetInventoryLotsResponse
                        {
                            InventoryLotId = x.InventoryLotId,
                            IngredientId = x.IngredientId,
                            IngredientCode = x.Ingredient.Code,
                            IngredientName = x.Ingredient.Name,
                            LotCode = x.LotCode,
                            ReceivedAt = x.ReceivedAt,
                            ExpiryDate = x.ExpiryDate,
                            OriginalQuantity = x.OriginalQuantity,
                            RemainingQuantity = x.RemainingQuantity,
                            UnitCost = x.UnitCost,
                            Unit = x.Ingredient.BaseUnit,
                            Status = resolvedStatus,
                            DaysRemaining = x.ExpiryDate.HasValue
                                ? (x.ExpiryDate.Value.Date - today).Days
                                : null,
                        };
                    }
                )
                .ToList();

            var result = new PagedResult<GetInventoryLotsResponse>(
                items,
                request.Pagination,
                pagedLots.TotalCount
            );

            await _cacheService.SetAsync(cacheKey, result, CacheTTL.Inventory, cancellationToken);

            _logger.LogInformation(
                "End handling GetInventoryLots with {Count} items out of {TotalCount}",
                result.Items.Count,
                result.TotalCount
            );

            return Result<PagedResult<GetInventoryLotsResponse>>.Success(result);
        }

        private static IQueryable<InventoryLot> ApplySorting(
            IQueryable<InventoryLot> query,
            string? orderBy
        )
        {
            var sortMapping = new Dictionary<string, Expression<Func<InventoryLot, object?>>>
            {
                { "lotcode", x => x.LotCode },
                { "ingredientname", x => x.Ingredient.Name },
                { "receivedat", x => x.ReceivedAt },
                { "expirydate", x => x.ExpiryDate },
                { "remainingquantity", x => x.RemainingQuantity },
                { "status", x => x.Status },
            };

            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return query
                    .OrderBy(x => x.ExpiryDate == null)
                    .ThenBy(x => x.ExpiryDate)
                    .ThenBy(x => x.ReceivedAt);
            }

            return query.ApplySorting(orderBy, sortMapping, x => x.ReceivedAt);
        }

        private static InventoryLotStatus ResolveStatus(
            InventoryLot lot,
            DateTime currentDate,
            int expiryWarningDays
        )
        {
            if (lot.Status == InventoryLotStatus.Disposed && lot.RemainingQuantity == 0)
            {
                return InventoryLotStatus.Disposed;
            }

            if (lot.RemainingQuantity <= 0)
            {
                return InventoryLotStatus.Depleted;
            }

            if (!lot.ExpiryDate.HasValue)
            {
                return InventoryLotStatus.Active;
            }

            if (lot.ExpiryDate.Value.Date < currentDate)
            {
                return InventoryLotStatus.Expired;
            }

            if (lot.ExpiryDate.Value.Date <= currentDate.AddDays(expiryWarningDays))
            {
                return InventoryLotStatus.NearExpiry;
            }

            return InventoryLotStatus.Active;
        }
    }
}
