using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlerts
{
    public record GetInventoryAlertsQuery() : IRequest<Result<GetInventoryAlertsResponse>>;
}
