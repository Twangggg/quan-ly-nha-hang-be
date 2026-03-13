using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings
{
    /// <summary>
    /// Retrieves the current inventory settings used by the system.
    /// </summary>
    public record GetInventorySettingsQuery() : IRequest<Result<GetInventorySettingsResponse>>;
}
