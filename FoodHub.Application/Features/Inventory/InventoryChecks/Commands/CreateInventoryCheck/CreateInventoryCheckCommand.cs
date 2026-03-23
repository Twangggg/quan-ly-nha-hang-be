using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck
{
    public class CreateInventoryCheckCommand
        : IRequest<Result<CreateInventoryCheckResponse>>, IMustBeActive
    {
        public DateTime CheckDate { get; set; }
        public List<CreateInventoryCheckItemDto> Items { get; set; } = new();
    }
}
