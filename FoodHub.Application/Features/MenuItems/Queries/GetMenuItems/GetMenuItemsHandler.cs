using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, Result<PagedResult<GetMenuItemsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetMenuItemsHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<PagedResult<GetMenuItemsResponse>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.MenuItemList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<PagedResult<GetMenuItemsResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<PagedResult<GetMenuItemsResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<MenuItem>().Query();

            // 1. Apply Global Search
            var searchableFields = new List<Expression<Func<MenuItem, string?>>>
            {
                m => m.Code,
                m => m.Name,
                m => m.Description
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<MenuItem, object?>>>
            {
                { "categoryId", m => m.CategoryId },
                { "station", m => m.Station },
                { "isOutOfStock", m => m.IsOutOfStock },
                { "priceDineIn", m => m.PriceDineIn }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<MenuItem, object?>>>
            {
                { "code", m => m.Code },
                { "name", m => m.Name },
                { "priceDineIn", m => m.PriceDineIn },
                { "priceTakeAway", m => m.PriceTakeAway },
                { "createdAt", m => m.CreatedAt }
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                m => m.Name);

            var pagedResult = await query
                .ProjectTo<GetMenuItemsResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.MenuItems, cancellationToken);
            return Result<PagedResult<GetMenuItemsResponse>>.Success(pagedResult);
        }
    }
}
