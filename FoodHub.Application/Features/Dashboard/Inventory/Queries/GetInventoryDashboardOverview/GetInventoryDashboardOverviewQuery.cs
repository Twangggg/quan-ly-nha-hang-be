using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Dashboard.Inventory.Queries.GetInventoryDashboardOverview
{
    public record GetInventoryDashboardOverviewQuery
        : IRequest<Result<GetInventoryDashboardOverviewResponse>>;
}
