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
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    public class GetAllAreasHandler
        : IRequestHandler<GetAllAreasQuery, Result<PagedResult<GetAllAreasResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAllAreasHandler> _logger;

        public GetAllAreasHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<GetAllAreasHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetAllAreasResponse>>> Handle(
            GetAllAreasQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Bắt đầu xử lý lấy danh sách khu vực.");

            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.AreaList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<PagedResult<GetAllAreasResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cachedResult != null)
            {
                _logger.LogInformation(
                    "Hoàn tất lấy danh sách khu vực (từ Cache). CacheKey: {CacheKey}",
                    cacheKey
                );
                return Result<PagedResult<GetAllAreasResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Area>().Query().AsNoTracking();

            // 1. Apply Global Search
            var searchableFields = new List<Expression<Func<Area, string?>>> { a => a.Name };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<Area, object?>>>
            {
                { "isActive", a => a.Status },
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<Area, object?>>>
            {
                { "name", a => a.Name },
                { "createdAt", a => a.CreatedAt },
            };

            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, a => a.Name);

            var pagedResult = await query
                .ProjectTo<GetAllAreasResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Areas, cancellationToken);
            _logger.LogInformation(
                "Hoàn tất lấy danh sách khu vực. Trả về {Count} kết quả (từ Database).",
                pagedResult.Items.Count
            );
            return Result<PagedResult<GetAllAreasResponse>>.Success(pagedResult);
        }
    }
}
