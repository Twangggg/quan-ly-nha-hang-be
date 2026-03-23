using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Dashboard.Orders.Queries.GetOrderDashboardOverview
{
    public record GetOrderDashboardOverviewQuery
        : IRequest<Result<GetOrderDashboardOverviewResponse>>;
}
