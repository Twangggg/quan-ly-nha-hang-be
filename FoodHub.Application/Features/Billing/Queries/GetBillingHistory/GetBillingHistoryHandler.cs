using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Billing.Queries.GetBillingHistory
{
    public class GetBillingHistoryHandler : IRequestHandler<GetBillingHistoryQuery, Result<PagedResult<GetBillingHistoryResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetBillingHistoryHandler> _logger;

        public GetBillingHistoryHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetBillingHistoryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetBillingHistoryResponse>>> Handle(GetBillingHistoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching billing history");

            // Only show orders that have been paid or completed
            var query = _unitOfWork.Repository<Order>().Query()
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed);

            var searchableFields = new List<Expression<Func<Order, string?>>>
            {
                o => o.OrderCode,
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"orderType", o => o.OrderType },
                {"paymentMethod", o => o.PaymentMethod! },
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Order, object?>>>
            {
                {"paidAt", o => o.PaidAt! },
                {"createdAt", o => o.CreatedAt },
                {"totalAmount", o => o.TotalAmount },
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, o => o.PaidAt!);

            var pagedResult = await query
                .ProjectTo<GetBillingHistoryResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetBillingHistoryResponse>>.Success(pagedResult);
        }
    }
}
