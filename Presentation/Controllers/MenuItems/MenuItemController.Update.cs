using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
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
            var result = await _mediator.Send(new UpdateMenuItemCommand(
                id,
                command.Code?.Trim(),
                command.Name?.Trim(),
                command.ImageUrl?.Trim(),
                command.Description?.Trim(),
                command.CategoryId,
                command.Station,
                command.ExpectedTime,
                command.PriceDineIn,
                command.PriceTakeAway,
                command.Cost
            ));

            return Ok(result);
        }
    }
}
