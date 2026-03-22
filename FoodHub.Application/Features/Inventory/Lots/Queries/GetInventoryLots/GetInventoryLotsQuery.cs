using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Lots.Queries.GetInventoryLots
{
    /// <summary>
    /// Query lay danh sach lo ton kho co phan trang va tim kiem.
    /// </summary>
    public record GetInventoryLotsQuery(PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetInventoryLotsResponse>>>;
}
