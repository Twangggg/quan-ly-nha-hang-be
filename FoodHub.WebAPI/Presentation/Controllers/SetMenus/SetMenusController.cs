using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenus;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;

namespace FoodHub.Presentation.Controllers
{
    [Tags("SetMenus")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class SetMenusController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SetMenusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetSetMenus([FromQuery] PaginationParams pagination)
        {
            var query = new GetSetMenusQuery { Pagination = pagination };
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSetMenuById(Guid id)
        {
            var query = new GetSetMenuByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> CreateSetMenu([FromBody] CreateSetMenuCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetSetMenuById), new { id = result.Data!.SetMenuId }, result.Data);
            }

            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> UpdateSetMenu(Guid id, [FromBody] UpdateSetMenuCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        [HttpPut("{id}/stock")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> UpdateSetMenuStockStatus(Guid id, [FromBody] UpdateSetMenuStockStatusCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        public async Task<IActionResult> DeleteSetMenu(Guid id)
        {
            var result = await _mediator.Send(new DeleteSetMenuCommand(id));
            return HandleResult(result);
        }
    }
}
