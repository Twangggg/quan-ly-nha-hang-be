using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks
{
    public class GetInventoryChecksHandler
        : IRequestHandler<GetInventoryChecksQuery, Result<PagedResult<GetInventoryChecksResponse>>>
    {
        private readonly ILogger<GetInventoryChecksHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetInventoryChecksHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<GetInventoryChecksHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryChecksResponse>>> Handle(
            GetInventoryChecksQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryChecks with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var cacheKey = CacheKeyBuilder.Build(CacheKey.InventoryChecksList, request);
            var cached = await _cacheService.GetAsync<PagedResult<GetInventoryChecksResponse>>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryChecks with {Count} items out of {TotalCount} (from cache)",
                    cached.Items.Count,
                    cached.TotalCount
                );

                return Result<PagedResult<GetInventoryChecksResponse>>.Success(cached);
            }

            var query = _unitOfWork.Repository<InventoryCheck>().Query().AsNoTracking();

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.CheckDate >= ToUtcStart(request.FromDate.Value));
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.CheckDate < ToUtcExclusiveEnd(request.ToDate.Value));
            }

            var orderedQuery = query
                .OrderByDescending(x => x.CheckDate)
                .ThenByDescending(x => x.CreatedAt);

            var totalCount = await orderedQuery.CountAsync(cancellationToken);

            var paginatedIds = await orderedQuery
                .Skip((request.Pagination.PageNumber - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .Select(x => x.InventoryCheckId)
                .ToListAsync(cancellationToken);

            var inventoryChecksWithItems = await _unitOfWork
                .Repository<InventoryCheck>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => paginatedIds.Contains(x.InventoryCheckId))
                .ToListAsync(cancellationToken);

            var createdByIds = inventoryChecksWithItems
                .Where(x => x.CreatedBy.HasValue)
                .Select(x => x.CreatedBy!.Value)
                .Distinct()
                .ToList();

            var employeeNameMap = await _unitOfWork
                .Repository<Employee>()
                .Query()
                .AsNoTracking()
                .Where(x => createdByIds.Contains(x.EmployeeId))
                .ToDictionaryAsync(x => x.EmployeeId, x => x.FullName, cancellationToken);

            var orderedChecks = paginatedIds
                .Select(id => inventoryChecksWithItems.First(x => x.InventoryCheckId == id))
                .ToList();

            var responses = orderedChecks
                .Select(x => new GetInventoryChecksResponse
                {
                    InventoryCheckId = x.InventoryCheckId,
                    CheckDate = x.CheckDate,
                    Status = x.Status,
                    CreatedByName = x.CreatedBy.HasValue
                        ? employeeNameMap.GetValueOrDefault(x.CreatedBy.Value)
                        : null,
                    TotalItems = x.Items.Count,
                })
                .ToList();

            var pagedResult = new PagedResult<GetInventoryChecksResponse>(
                responses,
                request.Pagination,
                totalCount
            );

            _logger.LogInformation(
                "End handling GetInventoryChecks with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            await _cacheService.SetAsync(
                cacheKey,
                pagedResult,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<PagedResult<GetInventoryChecksResponse>>.Success(pagedResult);
        }

        private static DateTime ToUtcStart(DateOnly value)
        {
            return DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }

        private static DateTime ToUtcExclusiveEnd(DateOnly value)
        {
            return DateTime.SpecifyKind(
                value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc
            );
        }
    }
}
