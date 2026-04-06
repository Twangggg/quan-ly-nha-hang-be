using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Queries.GetOrders
{
    public class GetOrdersHandler
        : IRequestHandler<GetOrdersQuery, Result<PagedResult<GetOrdersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetOrdersHandler> _logger;

        public GetOrdersHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetOrdersHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetOrdersResponse>>> Handle(
            GetOrdersQuery request,
            CancellationToken cancellationToken
        )
        {
            var query = _unitOfWork.Repository<Order>().Query();
            query = query
                .Include(o => o.Promotion)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                        .ThenInclude(og => og.OptionValues);

            var searchableFields = new List<Expression<Func<Order, string?>>> { o => o.OrderCode };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                { "orderType", o => o.OrderType },
                { "status", o => o.Status },
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                { "isPriority", o => o.IsPriority },
                { "completedAt", o => o.CompletedAt },
                { "createdAt", o => o.CreatedAt },
                { "totalAmount", o => o.TotalAmount },
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, o => o.OrderCode);

            var totalCount = await query.CountAsync(cancellationToken);
            
            var orders = await query
                .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .ToListAsync(cancellationToken);

            foreach (var order in orders)
            {
                order.RecalculateTotalAmount();
            }

            var mappedOrders = _mapper.Map<List<GetOrdersResponse>>(orders);
            
            var pagedResult = new PagedResult<GetOrdersResponse>(
                mappedOrders,
                request.Pagination,
                totalCount
            );

            return Result<PagedResult<GetOrdersResponse>>.Success(pagedResult);
        }
    }
}
