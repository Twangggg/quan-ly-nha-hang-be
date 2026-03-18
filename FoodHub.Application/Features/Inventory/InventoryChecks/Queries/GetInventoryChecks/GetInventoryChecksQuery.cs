using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks
{
    public record GetInventoryChecksQuery(
        PaginationParams Pagination,
        InventoryCheckStatus? Status,
        DateOnly? FromDate,
        DateOnly? ToDate
    ) : IRequest<Result<PagedResult<GetInventoryChecksResponse>>>, IMustBeActive;
}
