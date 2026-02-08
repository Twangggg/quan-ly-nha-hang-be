using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock; // Kept this as it was in original, but verified UpdateMenuItemStockStatusCommand usage in added methods
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItems;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Tags("MenuItems")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MenuItemsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet(Name = "GetMenuItems")]
        public async Task<IActionResult> GetMenuItems([FromQuery] PaginationParams pagination)
        {
            var query = new GetMenuItemsQuery { Pagination = pagination };
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMenuItemById(Guid id)
        {
            var query = new GetMenuItemByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateMenuItem([FromForm] CreateMenuItemCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return CreatedAtAction(nameof(GetMenuItemById), new { id = result.Data.MenuItemId }, result.Data);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenuItem(Guid id, UpdateMenuItemCommand command)
        {
            var result = await _mediator.Send(command with {MenuItemId = id});
            return Ok(result);
        }

        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateMenuItemStockStatus(Guid id, UpdateMenuItemStockStatusCommand command)
        {
            var result = await _mediator.Send(command with {MenuItemId = id});
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteMenuItemCommand(id));
            return Ok(result);
        }
    }
}
