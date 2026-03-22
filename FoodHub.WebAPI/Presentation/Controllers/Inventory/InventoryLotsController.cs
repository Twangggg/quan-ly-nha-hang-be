using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Inventory lot maintenance endpoints.
    /// </summary>
    [Tags("Kho hang - Lo ton kho")]
    public class InventoryLotsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryLotsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("/api/v{version:apiVersion}/inventory/lots/{id:guid}/dispose")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<DisposeInventoryLotResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> DisposeInventoryLot(
            Guid id,
            [FromBody] DisposeInventoryLotCommand command
        )
        {
            command.LotId = id;
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
