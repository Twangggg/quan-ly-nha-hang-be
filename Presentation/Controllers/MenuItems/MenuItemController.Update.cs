using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.MenuItems
{
    public partial class MenuItemController
    {
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMenuItem(Guid id, UpdateMenuItemCommand command)
        {
            var result = await _mediator.Send(command with {MenuItemId = id});
            return Ok(result);
        }

        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> ToggleOutOfStock(Guid id, ToggleOutOfStockCommand command)
        {
            var result = await _mediator.Send(command with {MenuItemId = id});
            return Ok(result);
        }
    }
}
