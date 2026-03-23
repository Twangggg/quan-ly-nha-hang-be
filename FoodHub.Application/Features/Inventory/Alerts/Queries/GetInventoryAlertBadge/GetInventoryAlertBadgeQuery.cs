using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlertBadge
{
    public record GetInventoryAlertBadgeQuery() : IRequest<Result<GetInventoryAlertBadgeResponse>>;
}
