using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
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
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients
{
    public class GetIngredientsHandler
        : IRequestHandler<GetIngredientsQuery, Result<PagedResult<GetIngredientsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetIngredientsHandler> _logger;

        public GetIngredientsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<GetIngredientsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetIngredientsResponse>>> Handle(
            GetIngredientsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetIngredients with search {Search}",
                request.Pagination.Search
            );

            try
            {
                var cacheKey = CacheKeyBuilder.Build(
                    CacheKey.InventoryIngredientsList,
                    request.Pagination
                );
                var cached = await _cacheService.GetAsync<PagedResult<GetIngredientsResponse>>(
                    cacheKey,
                    cancellationToken
                );
                if (cached is not null)
                {
                    _logger.LogInformation(
                        "End handling GetIngredients with {Count} items (from cache)",
                        cached.Items.Count
                    );
                    return Result<PagedResult<GetIngredientsResponse>>.Success(cached);
                }

                var query = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();

                // 1. Global Search
                var searchableFields = new List<Expression<Func<Ingredient, string?>>>
                {
                    x => x.Name,
                    x => x.Code,
                };
                query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

                // 2. Filters
                var normalizedFilters = request.Pagination.Filters ?? new List<string>();
                var stockStatusFilter = normalizedFilters
                    .Select(filter => filter.Split(':', 2))
                    .FirstOrDefault(parts =>
                        parts.Length == 2
                        && parts[0].Trim().Equals("status", StringComparison.OrdinalIgnoreCase)
                    )?[1]
                    ?.Trim();

                if (!string.IsNullOrWhiteSpace(stockStatusFilter))
                {
                    query = ApplyStockStatusFilter(query, stockStatusFilter);
                    normalizedFilters = normalizedFilters
                        .Where(filter =>
                        {
                            var parts = filter.Split(':', 2);
                            return parts.Length != 2
                                || !parts[0].Trim().Equals(
                                    "status",
                                    StringComparison.OrdinalIgnoreCase
                                );
                        })
                        .ToList();
                }

                var filterMapping = new Dictionary<string, Expression<Func<Ingredient, object?>>>
                {
                    { "isActive", x => x.IsActive },
                    { "unit", x => x.BaseUnit },
                };
                query = query.ApplyFilters(normalizedFilters, filterMapping);

                // 3. Sorting
                var sortMapping = new Dictionary<string, Expression<Func<Ingredient, object?>>>
                {
                    { "name", x => x.Name },
                    { "code", x => x.Code },
                    { "currentStock", x => x.CurrentStock },
                    { "createdAt", x => x.CreatedAt },
                };

                query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, x => x.Name);

                var pagedResult = await query
                    .ProjectTo<GetIngredientsResponse>(_mapper.ConfigurationProvider)
                    .ToPagedResultAsync(request.Pagination);

                _logger.LogInformation(
                    "End handling GetIngredients with {Count} items",
                    pagedResult.Items.Count
                );
                await _cacheService.SetAsync(
                    cacheKey,
                    pagedResult,
                    CacheTTL.Inventory,
                    cancellationToken
                );
                return Result<PagedResult<GetIngredientsResponse>>.Success(pagedResult);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting ingredients with search {Search}",
                    request.Pagination.Search
                );
                throw;
            }
        }

        private static IQueryable<Ingredient> ApplyStockStatusFilter(
            IQueryable<Ingredient> query,
            string rawStatus
        )
        {
            return rawStatus.ToUpperInvariant() switch
            {
                "OUT_OF_STOCK" => query.Where(x => x.CurrentStock == 0),
                "LOW_STOCK" => query.Where(
                    x => x.CurrentStock > 0 && x.CurrentStock <= x.LowStockThreshold
                ),
                "NORMAL" => query.Where(x => x.CurrentStock > x.LowStockThreshold),
                _ => query,
            };
        }
    }
}
