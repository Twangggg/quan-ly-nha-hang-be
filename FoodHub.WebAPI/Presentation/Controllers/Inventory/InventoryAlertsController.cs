using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlertBadge;
using FoodHub.Application.Features.Inventory.Alerts.Queries.GetInventoryAlerts;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Inventory alerts for stock level and expiry monitoring.
    /// </summary>
    [Tags("Kho hang - Canh bao")]
    public class InventoryAlertsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryAlertsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/alerts")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(Result<GetInventoryAlertsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInventoryAlerts()
        {
            var result = await _mediator.Send(new GetInventoryAlertsQuery());
            return HandleResult(result);
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/alerts/badge")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetInventoryAlertBadgeResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryAlertBadge()
        {
            var result = await _mediator.Send(new GetInventoryAlertBadgeQuery());
            return HandleResult(result);
        }
    }
}
