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
            var queryJson = JsonSerializer.Serialize(request.Pagination);
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
                { "isOutOfStock", s => s.IsOutOfStock },
                { "price", s => s.Price }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

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
