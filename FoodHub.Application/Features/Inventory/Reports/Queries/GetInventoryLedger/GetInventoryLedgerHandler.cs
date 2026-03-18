using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public class GetInventoryLedgerHandler
        : IRequestHandler<GetInventoryLedgerQuery, Result<IReadOnlyList<GetInventoryLedgerResponse>>>
    {
        private readonly ILogger<GetInventoryLedgerHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public GetInventoryLedgerHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetInventoryLedgerHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<GetInventoryLedgerResponse>>> Handle(
            GetInventoryLedgerQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryLedger for IngredientId={IngredientId} from {FromDate} to {ToDate}",
                request.IngredientId,
                request.FromDate,
                request.ToDate
            );

            var from = ToUtcStart(request.FromDate);
            var toExclusive = ToUtcExclusiveEnd(request.ToDate);

            var query = _unitOfWork
                .Repository<InventoryTransaction>()
                .Query()
                .AsNoTracking()
                .Where(
                    x =>
                        x.IngredientId == request.IngredientId
                        && x.OccurredAt >= from
                        && x.OccurredAt < toExclusive
                );

            if (request.TransactionType.HasValue)
            {
                query = query.Where(x => x.TransactionType == request.TransactionType.Value);
            }

            var responses = await query
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new GetInventoryLedgerResponse
                {
                    OccurredAt = x.OccurredAt,
                    TransactionType = x.TransactionType,
                    ReferenceNo = x.Reference,
                    QuantityDelta = x.Quantity,
                    BalanceAfter = x.BalanceAfter,
                    Note = null,
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "End handling GetInventoryLedger with {Count} items",
                responses.Count
            );

            return Result<IReadOnlyList<GetInventoryLedgerResponse>>.Success(responses);
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
