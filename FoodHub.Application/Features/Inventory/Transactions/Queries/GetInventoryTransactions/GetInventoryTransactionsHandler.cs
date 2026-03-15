using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Transactions.Queries.GetInventoryTransactions
{
    public class GetInventoryTransactionsHandler
        : IRequestHandler<
            GetInventoryTransactionsQuery,
            Result<PagedResult<GetInventoryTransactionsResponse>>
        >
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetInventoryTransactionsHandler> _logger;

        public GetInventoryTransactionsHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetInventoryTransactionsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetInventoryTransactionsResponse>>> Handle(
            GetInventoryTransactionsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryTransactions with PageNumber={PageNumber}, PageSize={PageSize}",
                request.Pagination.PageNumber,
                request.Pagination.PageSize
            );

            var query = _unitOfWork.Repository<InventoryTransaction>().Query().AsNoTracking();

            query = query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.CreatedAt);

            var projection = query.Select(x => new GetInventoryTransactionsResponse
            {
                InventoryTransactionId = x.InventoryTransactionId,
                IngredientId = x.IngredientId,
                IngredientName = x.Ingredient.Name,
                IngredientCode = x.Ingredient.Code,
                TransactionType = x.TransactionType,
                Quantity = x.Quantity,
                UnitCost = x.UnitCost,
                BalanceAfter = x.BalanceAfter,
                Reference = x.Reference,
                OccurredAt = x.OccurredAt,
            });

            var pagedResult = await projection.ToPagedResultAsync(
                request.Pagination,
                cancellationToken
            );

            _logger.LogInformation(
                "End handling GetInventoryTransactions with {Count} items out of {TotalCount}",
                pagedResult.Items.Count,
                pagedResult.TotalCount
            );

            return Result<PagedResult<GetInventoryTransactionsResponse>>.Success(pagedResult);
        }
    }
}
