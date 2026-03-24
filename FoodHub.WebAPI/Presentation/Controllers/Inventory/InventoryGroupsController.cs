using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Groups.Commands.CreateInventoryGroup;
using FoodHub.Application.Features.Inventory.Groups.Commands.DeleteInventoryGroup;
using FoodHub.Application.Features.Inventory.Groups.Commands.UpdateInventoryGroup;
using FoodHub.Application.Features.Inventory.Groups.Queries.GetInventoryGroups;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Kho hang - Nhom nguyen lieu")]
    public class InventoryGroupsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryGroupsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/groups")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(Result<List<GetInventoryGroupsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInventoryGroups()
        {
            var result = await _mediator.Send(new GetInventoryGroupsQuery());
            return HandleResult(result);
        }

        [HttpPost("/api/v{version:apiVersion}/inventory/groups")]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(typeof(Result<CreateInventoryGroupResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateInventoryGroup([FromBody] CreateInventoryGroupCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleCreated(
                result,
                data => Url.Action(nameof(GetInventoryGroups))
            );
        }

        [HttpPut("/api/v{version:apiVersion}/inventory/groups/{id:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<UpdateInventoryGroupResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateInventoryGroup(
            Guid id,
            [FromBody] UpdateInventoryGroupCommand command
        )
        {
            command = command with { InventoryGroupId = id };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("/api/v{version:apiVersion}/inventory/groups/{id:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<DeleteInventoryGroupResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteInventoryGroup(Guid id)
        {
            var result = await _mediator.Send(new DeleteInventoryGroupCommand(id));
            return HandleResult(result);
        }
    }
}
