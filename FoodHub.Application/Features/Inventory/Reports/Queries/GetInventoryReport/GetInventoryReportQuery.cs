using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport
{
    public record GetInventoryReportQuery(
        PaginationParams Pagination,
        DateOnly FromDate,
        DateOnly ToDate,
        Guid? IngredientId
    ) : IRequest<Result<PagedResult<GetInventoryReportResponse>>>, IMustBeActive;
}
