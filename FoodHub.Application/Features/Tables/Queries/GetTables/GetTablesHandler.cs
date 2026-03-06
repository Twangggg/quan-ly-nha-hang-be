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
    public class GetTablesHandler : IRequestHandler<GetTablesQuery, Result<List<GetTablesResponse>>>
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
        public async Task<Result<List<GetTablesResponse>>> Handle(
            GetTablesQuery request,
            CancellationToken cancellationToken
        )
        {
            var cacheKey = request.AreaId.HasValue 
                ? string.Format(CacheKey.TableListByArea, request.AreaId) 
                : string.Format(CacheKey.TableList);

            var cachedResult = await _cacheService.GetAsync<List<GetTablesResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<List<GetTablesResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Table>().Query();

            if (request.AreaId.HasValue && request.AreaId.Value != Guid.Empty)
            {
                query = query.Where(t => t.AreaId == request.AreaId.Value);
            }

            var tables = await query
                .Include(a => a.Area)
                .OrderBy(t => t.TableNumber)
                .ProjectTo<GetTablesResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, tables, CacheTTL.Tables, cancellationToken);
            return Result<List<GetTablesResponse>>.Success(tables);
        }
    }

}
