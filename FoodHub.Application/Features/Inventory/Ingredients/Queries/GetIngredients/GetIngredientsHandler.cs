using System.Linq.Expressions;
using AutoMapper;
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
using FoodHub.Domain.Services;
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
        private readonly IInventoryRuleResolver _inventoryRuleResolver;
        private readonly ILogger<GetIngredientsHandler> _logger;

        public GetIngredientsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IInventoryRuleResolver inventoryRuleResolver,
            ILogger<GetIngredientsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _inventoryRuleResolver = inventoryRuleResolver;
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

                var query = _unitOfWork
                    .Repository<Ingredient>()
                    .Query()
                    .Include(x => x.InventoryGroup)
                    .AsNoTracking();

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

                var settings =
                    await _unitOfWork
                        .Repository<InventorySettings>()
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.SettingsKey == InventorySettings.DefaultSettingsKey,
                            cancellationToken
                        ) ?? InventorySettings.CreateDefault();

                var ingredients = await query.ToListAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(stockStatusFilter))
                {
                    ingredients = ApplyStockStatusFilter(
                        ingredients,
                        stockStatusFilter,
                        settings
                    );
                }

                var totalCount = ingredients.Count;
                var pageItems = ingredients
                    .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                    .Take(request.Pagination.PageSize)
                    .Select(ingredient => MapIngredient(ingredient, settings))
                    .ToList();

                var pagedResult = new PagedResult<GetIngredientsResponse>(
                    pageItems,
                    request.Pagination,
                    totalCount
                );

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

        private List<Ingredient> ApplyStockStatusFilter(
            IEnumerable<Ingredient> ingredients,
            string rawStatus,
            InventorySettings globalSettings
        )
        {
            return rawStatus.ToUpperInvariant() switch
            {
                "OUT_OF_STOCK" =>
                    ingredients.Where(x =>
                        x.GetStockStatus(
                            _inventoryRuleResolver.Resolve(x, globalSettings).LowStockThreshold
                        ) == StockStatus.OutOfStock
                    ).ToList(),
                "LOW_STOCK" =>
                    ingredients.Where(x =>
                        x.GetStockStatus(
                            _inventoryRuleResolver.Resolve(x, globalSettings).LowStockThreshold
                        ) == StockStatus.LowStock
                    ).ToList(),
                "NORMAL" =>
                    ingredients.Where(x =>
                        x.GetStockStatus(
                            _inventoryRuleResolver.Resolve(x, globalSettings).LowStockThreshold
                        ) == StockStatus.Normal
                    ).ToList(),
                _ => ingredients.ToList(),
            };
        }

        private GetIngredientsResponse MapIngredient(
            Ingredient ingredient,
            InventorySettings globalSettings
        )
        {
            var resolvedRules = _inventoryRuleResolver.Resolve(ingredient, globalSettings);
            var response = _mapper.Map<GetIngredientsResponse>(ingredient);
            response.LowStockThreshold = resolvedRules.LowStockThreshold;
            response.UseDefaultLowStockThreshold = ingredient.UseDefaultLowStockThreshold;
            response.InventoryGroupName = ingredient.InventoryGroup?.Name;
            response.StockStatus = ingredient.GetStockStatus(resolvedRules.LowStockThreshold)
                .ToString();
            return response;
        }
    }
}
