using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Queries.GetVouchers
{
    public class GetVouchersHandler : IRequestHandler<GetVouchersQuery, Result<PagedResult<GetVouchersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetVouchersHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetVouchersHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetVouchersHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }
        public async Task<Result<PagedResult<GetVouchersResponse>>> Handle(GetVouchersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetVouchersQuery with Pagination: {@Pagination}", request.Pagination);

            var paginationKey = JsonSerializer.Serialize(request.Pagination);
            var cacheKey = string.Format(CacheKey.VoucherListPagination, paginationKey);
            var cachedData = await _cacheService.GetAsync<PagedResult<GetVouchersResponse>>(cacheKey);
            if (cachedData != null)
            {
                return Result<PagedResult<GetVouchersResponse>>.Success(cachedData);
            }

            var query = _unitOfWork.Repository<Voucher>().Query();

            var searchableFields = new List<Expression<Func<Voucher, string?>>> {
                v => v.VoucherCode
             };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            var filterMapping = new Dictionary<string, Expression<Func<Voucher, object>>>(StringComparer.OrdinalIgnoreCase)
            {
                { "voucherType", v => v.VoucherType },
                { "isActive", v => v.IsActive }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            var sortMapping = new Dictionary<string, Expression<Func<Voucher, object>>>()
            {
                { "voucherCode", v => v.VoucherCode },
                { "startDate", v => v.StartDate },
                { "endDate", v => v.EndDate }
            };
            query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, v => v.VoucherCode);

            var pagedResults = await query
                .ProjectTo<GetVouchersResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);
            await _cacheService.SetAsync(cacheKey, pagedResults, CacheTTL.Vouchers, cancellationToken);
            return Result<PagedResult<GetVouchersResponse>>.Success(pagedResults);
        }
    }
}
