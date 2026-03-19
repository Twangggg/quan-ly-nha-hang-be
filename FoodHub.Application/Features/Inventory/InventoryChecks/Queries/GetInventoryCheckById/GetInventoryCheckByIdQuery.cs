using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckById
{
    public record GetInventoryCheckByIdQuery(Guid InventoryCheckId)
        : IRequest<Result<GetInventoryCheckByIdResponse>>;
}
