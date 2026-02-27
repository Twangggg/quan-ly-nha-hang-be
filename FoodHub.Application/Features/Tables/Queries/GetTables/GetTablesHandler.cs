using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Tables.Queries.GetTables
{
    public class GetTablesHandler : IRequestHandler<GetTablesQuery, Result<PagedResult<GetTablesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetTablesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }
        public async Task<Result<PagedResult<GetTablesResponse>>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.TableList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<Result<PagedResult<GetTablesResponse>>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var query = _unitOfWork.Repository<Table>().Query();

            var searchableFields = new List<Expression<Func<Table, string?>>>
            {
                t => t.TableCode,
                t => t.Area.ToString()
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Table, object>>>
            {
                {"status", t => t.Status},
                {"areaId", t => t.AreaId},
                {"capacity", t => t.Capacity}
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Table, object>>>
            {
                {"tableCode", t => t.TableCode},
                {"capacity", t => t.Capacity},
                {"createdAt", t => t.CreatedAt}
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, r => r.TableCode);

            var pagedResult = await query
                .ProjectTo<GetTablesResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Tables, cancellationToken);
            return Result<PagedResult<GetTablesResponse>>.Success(pagedResult);
        }
    }
}
