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

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, Result<PagedResult<GetCategoriesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetAllCategoriesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<PagedResult<GetCategoriesResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.CategoryList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<PagedResult<GetCategoriesResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<PagedResult<GetCategoriesResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Category>().Query();

            // 1. Apply Global Search
            var searchableFields = new List<Expression<Func<Category, string?>>>
            {
                c => c.Name
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<Category, object?>>>
            {
                { "type", c => c.CategoryType },
                { "isActive", c => c.IsActive }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<Category, object?>>>
            {
                { "name", c => c.Name },
                { "createdAt", c => c.CreatedAt }
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                c => c.Name);

            var pagedResult = await query
                .ProjectTo<GetCategoriesResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Categories, cancellationToken);
            return Result<PagedResult<GetCategoriesResponse>>.Success(pagedResult);
        }
    }
}
