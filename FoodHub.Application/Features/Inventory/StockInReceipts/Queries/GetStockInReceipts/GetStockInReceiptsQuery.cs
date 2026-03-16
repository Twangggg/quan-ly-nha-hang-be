using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts
{
    public record GetStockInReceiptsQuery(
        PaginationParams Pagination,
        DateOnly? FromDate,
        DateOnly? ToDate
    ) : IRequest<Result<PagedResult<GetStockInReceiptsResponse>>>;
}
