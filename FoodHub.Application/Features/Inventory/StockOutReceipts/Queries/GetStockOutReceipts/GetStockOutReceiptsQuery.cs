using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts
{
    public record GetStockOutReceiptsQuery(
        PaginationParams Pagination,
        DateOnly? FromDate,
        DateOnly? ToDate
    ) : IRequest<Result<PagedResult<GetStockOutReceiptsResponse>>>;
}
