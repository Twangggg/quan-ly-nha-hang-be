using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public class GetStockInReceiptsHandler
        : IRequestHandler<
            GetStockInReceiptsQuery,
            Result<PagedResult<GetStockInReceiptsResponse>>
        >
    {
        private readonly ILogger<GetStockInReceiptsHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public GetStockInReceiptsHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStockInReceiptsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetStockInReceiptsResponse>>> Handle(
            GetStockInReceiptsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetStockInReceipts with PageNumber={PageNumber}, PageSize={PageSize}",
                request.PageNumber,
                request.PageSize
            );

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();
            var query = _unitOfWork.Repository<StockInReceipt>().Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim();
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
                request.ToPaginationParams(),
                cancellationToken
            );

            _logger.LogInformation(
                "End handling GetStockInReceipts with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            return Result<PagedResult<GetStockInReceiptsResponse>>.Success(pagedResult);
        }

        private static DateTime ToUtcDateTime(DateOnly value)
        {
            return DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }
    }
}
