using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.SetMenus
{
    public partial class SetMenuController
    {
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSetMenu(Guid id)
        {
            var result = await _mediator.Send(new DeleteSetMenuCommand(id));
            return Ok(result);
        }
    }
}
