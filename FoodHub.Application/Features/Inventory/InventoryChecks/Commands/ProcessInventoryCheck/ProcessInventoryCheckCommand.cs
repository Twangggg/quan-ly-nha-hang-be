using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck
{
    public record ProcessInventoryCheckCommand(Guid InventoryCheckId)
        : IRequest<Result<ProcessInventoryCheckResponse>>, IMustBeActive;
}
