using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;

namespace FoodHub.Application.Features.Orders.Queries.GetOrders
{
    public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, Result<PagedResult<GetOrdersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetOrdersQuery> _logger;

        public GetOrdersHandler(IUnitOfWork unitOfWork, IMapper mapper,
            ICacheService cacheService, ILogger<GetOrdersQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetOrdersResponse>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            // Normalize caching across handlers
            var queryJson = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = $"{CacheKey.OrderList}:{queryJson.GetHashCode()}";

            var cachedResult = await _cacheService.GetAsync<PagedResult<GetOrdersResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<PagedResult<GetOrdersResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Order>().Query();

            var searchableFields = new List<Expression<Func<Order, string?>>>
            {
                o => o.OrderCode,
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"orderType", o => o.OrderType },
                {"status", o => o.Status}
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"isPriority", o => o.IsPriority },
                {"completedAt", o => o.CompletedAt },
                {"createdAt", o => o.CreatedAt },
                {"totalAmount", o => o.TotalAmount }
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, o => o.OrderCode);

            var pagedResult = await query
                .ProjectTo<GetOrdersResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Orders, cancellationToken);
            return Result<PagedResult<GetOrdersResponse>>.Success(pagedResult);
        }
    }
}
