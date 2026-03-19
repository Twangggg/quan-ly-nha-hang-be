using FoodHub.Application.Common.Models;
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

        public GetInventoryChecksHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetInventoryChecksHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
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

            var projection = query
                .OrderByDescending(x => x.CheckDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new GetInventoryChecksResponse
                {
                    InventoryCheckId = x.InventoryCheckId,
                    CheckDate = x.CheckDate,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    TotalItems = x.Items.Count,
                });

            var pagedResult = await projection.ToPagedResultAsync(
                request.Pagination,
                cancellationToken
            );

            _logger.LogInformation(
                "End handling GetInventoryChecks with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
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
