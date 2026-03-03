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
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    /// <summary>
    /// Handler for retrieving a paginated list of tables with support for global search, filtering, and sorting.
    /// </summary>
    public class GetTablesHandler : IRequestHandler<GetTablesQuery, Result<PagedResult<GetTablesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Constructor to inject dependencies for the GetTablesHandler, including database access, mapping, and caching services.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="mapper"></param>
        /// <param name="cacheService"></param>
        public GetTablesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Handles the GetTablesQuery by applying global search, filters, and sorting to the Table entities, then returns a paginated result. The results are cached to improve performance for subsequent requests with the same parameters.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<PagedResult<GetTablesResponse>>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
        {
            // Generate a cache key based on the pagination parameters to store and retrieve cached results
            var queryJson = JsonSerializer.Serialize(new { request.Pagination});
            var cacheKey = $"{CacheKey.TableList}:{queryJson.GetHashCode()}";

            // Attempt to retrieve the result from cache first to avoid unnecessary database queries
            var cachedResult = await _cacheService.GetAsync<Result<PagedResult<GetTablesResponse>>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            // Start building the query for Table entities
            var query = _unitOfWork.Repository<Table>().Query();

            // Define the fields that should be included in the global search functionality
            var searchableFields = new List<Expression<Func<Table, string?>>>
            {
                t => t.TableNumber.ToString(),
                t => t.Area.Name
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // Define the mapping of filter keys to their corresponding entity properties for dynamic filtering
            var filterMapping = new Dictionary<string, Expression<Func<Table, object>>>
            {
                {"status", t => t.Status},
                {"areaId", t => t.AreaId},
                {"capacity", t => t.Capacity}
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // Define the mapping of sort keys to their corresponding entity properties for dynamic sorting
            var sortMapping = new Dictionary<string, Expression<Func<Table, object>>>
            {
                {"tableNumber", t => t.TableNumber},
                {"capacity", t => t.Capacity},
                {"createdAt", t => t.CreatedAt}
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, r => r.TableNumber);
            // Project the query results to the GetTablesResponse DTO and apply pagination before returning the result
            var pagedResult = await query
                .Include(a => a.Area) // Include related Area entity for mapping to response DTO
                .ProjectTo<GetTablesResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            // Store the result in cache for future requests with the same parameters to improve performance
            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Tables, cancellationToken);
            return Result<PagedResult<GetTablesResponse>>.Success(pagedResult);
        }
    }
}
