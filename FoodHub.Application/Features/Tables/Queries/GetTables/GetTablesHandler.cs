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

        public GetTablesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetTablesResponse>>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
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
