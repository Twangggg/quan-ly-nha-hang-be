using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Transactions.Queries.GetInventoryTransactions
{
    public record GetInventoryTransactionsQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetInventoryTransactionsResponse>>>;
}
