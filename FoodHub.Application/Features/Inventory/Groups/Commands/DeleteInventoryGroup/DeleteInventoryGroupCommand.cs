using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Groups.Commands.DeleteInventoryGroup
{
    public sealed record DeleteInventoryGroupCommand(Guid InventoryGroupId)
        : IRequest<Result<DeleteInventoryGroupResponse>>;
}
