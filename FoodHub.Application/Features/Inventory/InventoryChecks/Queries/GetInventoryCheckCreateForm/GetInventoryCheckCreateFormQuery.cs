using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm
{
    public record GetInventoryCheckCreateFormQuery()
        : IRequest<Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>>, IMustBeActive;
}
