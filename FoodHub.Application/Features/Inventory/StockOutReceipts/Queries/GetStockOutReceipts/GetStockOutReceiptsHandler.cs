using FoodHub.Application.Common.Models;
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

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts
{
    public class GetStockOutReceiptsHandler
        : IRequestHandler<
            GetStockOutReceiptsQuery,
            Result<PagedResult<GetStockOutReceiptsResponse>>
        >
    {
        private readonly ILogger<GetStockOutReceiptsHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public GetStockOutReceiptsHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStockOutReceiptsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetStockOutReceiptsResponse>>> Handle(
            GetStockOutReceiptsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetStockOutReceipts with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();
            var query = _unitOfWork.Repository<StockOutReceipt>().Query().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
            {
                var keyword = request.Pagination.Search.Trim();
                query = query.Where(x => x.ReceiptCode.Contains(keyword));
            }

            if (request.FromDate.HasValue)
            {
                var fromDate = ToUtcDateTime(request.FromDate.Value);
                query = query.Where(x => x.StockOutDate >= fromDate);
            }

            if (request.ToDate.HasValue)
            {
                var toDateExclusive = ToUtcDateTime(request.ToDate.Value.AddDays(1));
                query = query.Where(x => x.StockOutDate < toDateExclusive);
            }

            var projection = query
                .OrderByDescending(x => x.StockOutDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new GetStockOutReceiptsResponse
                {
                    StockOutReceiptId = x.StockOutReceiptId,
                    ReceiptCode = x.ReceiptCode,
                    StockOutDate = x.StockOutDate,
                    TotalAmount = x.TotalAmount,
                    Reason = x.Reason,
                    TotalItems = x.TotalItems,
                    CreatedByName = employeeQuery
                        .Where(e => e.EmployeeId == x.CreatedBy)
                        .Select(e => e.FullName)
                        .FirstOrDefault(),
                });

            var pagedResult = await projection.ToPagedResultAsync(
                request.Pagination,
                cancellationToken
            );

            return Result<PagedResult<GetStockOutReceiptsResponse>>.Success(pagedResult);
        }

        private static DateTime ToUtcDateTime(DateOnly value)
        {
            return DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        }
    }
}
