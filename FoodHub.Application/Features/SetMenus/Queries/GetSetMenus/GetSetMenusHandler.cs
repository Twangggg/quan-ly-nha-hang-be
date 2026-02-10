using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using System.Linq.Expressions;
using System.Text.Json;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusHandler : IRequestHandler<GetSetMenusQuery, Result<PagedResult<GetSetMenusResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetSetMenusHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<PagedResult<GetSetMenusResponse>>> Handle(GetSetMenusQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(new
            {
                request.Pagination.Search,
                request.Pagination.Filters,
                request.Pagination.OrderBy,
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            });
            var cacheKey = $"{CacheKey.SetMenuList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<PagedResult<GetSetMenusResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<PagedResult<GetSetMenusResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<SetMenu>().Query();

            // 1. Apply Global Search (search by Code, Name, Description)
            var searchableFields = new List<Expression<Func<SetMenu, string?>>>
            {
                s => s.Code,
                s => s.Name,
                s => s.Description
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<SetMenu, object?>>>
            {
                { "isOutOfStock", s => s.IsOutOfStock }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // Apply Price Range Filter
            if (request.Pagination.Filters != null)
            {
                foreach (var filter in request.Pagination.Filters)
                {
                    var parts = filter.Split(':');
                    if (parts.Length < 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (key.Equals("minPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var minPrice))
                    {
                        query = query.Where(s => s.Price >= minPrice);
                    }
                    else if (key.Equals("maxPrice", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(value, out var maxPrice))
                    {
                        query = query.Where(s => s.Price <= maxPrice);
                    }
                }
            }

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<SetMenu, object?>>>
            {
                { "code", s => s.Code },
                { "name", s => s.Name },
                { "price", s => s.Price },
                { "createdAt", s => s.CreatedAt }
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                s => s.Name);

            var pagedResult = await query
                .ProjectTo<GetSetMenusResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.SetMenus, cancellationToken);
            return Result<PagedResult<GetSetMenusResponse>>.Success(pagedResult);
        }
    }
}
