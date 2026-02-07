using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;
using FoodHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.SetMenus
{
    public partial class SetMenuController
    {
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        public async Task<IActionResult> UpdateSetMenu(Guid id, [FromBody] UpdateSetMenuCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });

            return Ok(result);
        }

        [HttpPut("{id}/stock")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        public async Task<IActionResult> UpdateSetMenuStockStatus(Guid id, [FromBody] UpdateSetMenuStockStatusCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });

            return Ok(result);
        }
    }
}
