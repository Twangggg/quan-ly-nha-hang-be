using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList
{
    public record GetOpeningStockListQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetOpeningStockListResponse>>>;
}
