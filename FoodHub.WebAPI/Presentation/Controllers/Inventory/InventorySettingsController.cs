using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock;
using FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList;
using FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings;
using FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Kho hang - Cau hinh kho")]
    public class InventorySettingsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventorySettingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/settings")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetInventorySettingsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventorySettings()
        {
            var result = await _mediator.Send(new GetInventorySettingsQuery());
            return HandleResult(result);
        }

        [HttpPut("/api/v{version:apiVersion}/inventory/settings")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<UpdateInventorySettingsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> UpdateInventorySettings(
            [FromBody] UpdateInventorySettingsCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/opening-stock")]
        [HttpGet("/api/v{version:apiVersion}/inventory/open-stocking")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetOpeningStockListResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetOpeningStockList(
            [FromQuery] PaginationParams pagination
        )
        {
            var result = await _mediator.Send(new GetOpeningStockListQuery(pagination));
            return HandleResult(result);
        }

        [HttpPost("/api/v{version:apiVersion}/inventory/opening-stock")]
        [HttpPost("/api/v{version:apiVersion}/inventory/open-stocking")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<ImportOpeningStockResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportOpeningStock(
            [FromBody] ImportOpeningStockCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
