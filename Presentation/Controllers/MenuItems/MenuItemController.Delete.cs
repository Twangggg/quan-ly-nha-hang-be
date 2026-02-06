using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FoodHub.Presentation.Controllers.MenuItems
{
    public partial class MenuItemController
    {
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteMenuItemCommand(id));
            return Ok(result);
        }
    }
}
