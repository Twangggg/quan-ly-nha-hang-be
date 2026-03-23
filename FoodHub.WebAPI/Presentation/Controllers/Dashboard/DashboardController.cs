using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Dashboard.Inventory.Queries.GetInventoryDashboardOverview;
using FoodHub.Application.Features.Dashboard.Orders.Queries.GetOrderDashboardOverview;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Dashboard")]
    public class DashboardController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("orders/overview")]
        [HasPermission(Permissions.Orders.View)]
        [ProducesResponseType(
            typeof(Result<GetOrderDashboardOverviewResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetOrderOverview()
        {
            var result = await _mediator.Send(new GetOrderDashboardOverviewQuery());
            return HandleResult(result);
        }

        [HttpGet("inventory/overview")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetInventoryDashboardOverviewResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryOverview()
        {
            var result = await _mediator.Send(new GetInventoryDashboardOverviewQuery());
            return HandleResult(result);
        }
    }
}
