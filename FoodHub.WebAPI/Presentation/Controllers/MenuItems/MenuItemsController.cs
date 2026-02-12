using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock; // Kept this as it was in original, but verified UpdateMenuItemStockStatusCommand usage in added methods
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItems;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("MenuItems")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class MenuItemsController : ApiControllerBase
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
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMenuItemById(Guid id)
        {
            var query = new GetMenuItemByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [HasPermission(Permissions.MenuItems.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> CreateMenuItem([FromForm] CreateMenuItemCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetMenuItemById),
                    new { id = result.Data!.MenuItemId },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.MenuItems.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> UpdateMenuItem(Guid id, UpdateMenuItemCommand command)
        {
            var result = await _mediator.Send(command with { MenuItemId = id });
            return HandleResult(result);
        }

        [HttpPut("{id}/stock")]
        [HasPermission(Permissions.MenuItems.UpdateStock)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> UpdateMenuItemStockStatus(
            Guid id,
            UpdateMenuItemStockStatusCommand command
        )
        {
            var result = await _mediator.Send(command with { MenuItemId = id });
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.MenuItems.Delete)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteMenuItemCommand(id));
            return HandleResult(result);
        }
    }
}
