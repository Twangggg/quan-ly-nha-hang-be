using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace FoodHub.Application.Features.Orders.Queries.GetOrders
{
    public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, Result<PagedResult<GetOrdersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetOrdersQuery> _logger;

        public GetOrdersHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetOrdersQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetOrdersResponse>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<Order>().Query();

            var searchableFields = new List<Expression<Func<Order, string?>>>
            {
                o => o.OrderCode,
                //o => o.TableId
            };
            query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"orderType", o => o.OrderType },
                {"status", o => o.Status}
            };
            query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"isPriority", o => o.IsPriority },
                {"completedAt", o => o.CompletedAt }
            };
            query.ApplySorting(request.Pagination.OrderBy, sortMapping, o => o.OrderCode);

            var pagedResult = await query
                .ProjectTo<GetOrdersResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetOrdersResponse>>.Success(pagedResult);
        }
    }
}
