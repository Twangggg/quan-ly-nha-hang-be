using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public class GetStockInReceiptsHandler
        : IRequestHandler<GetStockInReceiptsQuery, Result<PagedResult<GetStockInReceiptsResponse>>>
    {
        private readonly ILogger<GetStockInReceiptsHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetStockInReceiptsHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetStockInReceiptsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetStockInReceiptsResponse>>> Handle(
            GetStockInReceiptsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetStockInReceipts with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryStockInReceiptsList, request);
            var cached = await _cacheService.GetAsync<PagedResult<GetStockInReceiptsResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetStockInReceipts with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );
                return Result<PagedResult<GetStockInReceiptsResponse>>.Success(cached);
            }

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();
            var query = _unitOfWork.Repository<StockInReceipt>().Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
            {
                var keyword = request.Pagination.Search.Trim();
                query = query.Where(x => x.ReceiptCode.Contains(keyword));
            }

            if (request.FromDate.HasValue)
            {
                var fromDate = ToUtcDateTime(request.FromDate.Value);
                query = query.Where(x => x.ReceivedAt >= fromDate);
            }

            if (request.ToDate.HasValue)
            {
                var toDateExclusive = ToUtcDateTime(request.ToDate.Value.AddDays(1));
                query = query.Where(x => x.ReceivedAt < toDateExclusive);
            }

            var projection = query
                .OrderByDescending(x => x.ReceivedAt)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new GetStockInReceiptsResponse
                {
                    StockInReceiptId = x.StockInReceiptId,
                    ReceiptCode = x.ReceiptCode,
                    ReceivedAt = x.ReceivedAt,
                    TotalLines = x.TotalLines,
                    TotalAmount = x.TotalAmount,
                    Note = x.Note,
                    CreatedByName = employeeQuery
                        .Where(e => e.EmployeeId == x.CreatedBy)
                        .Select(e => e.FullName)
                        .FirstOrDefault(),
                });

            var pagedResult = await projection.ToPagedResultAsync(
                request.Pagination,
                cancellationToken
            );

            _logger.LogInformation(
                "End handling GetStockInReceipts with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<PagedResult<GetStockInReceiptsResponse>>.Success(pagedResult);
        }

        private static DateTime ToUtcDateTime(DateOnly value)
        {
            return DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }
    }
}
