using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.SetMenus
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SetMenuController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SetMenuController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetSetMenus(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetSetMenusQuery(pageNumber, pageSize);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSetMenuById(Guid id)
        {
            var query = new GetSetMenuByIdQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateSetMenu([FromBody] CreateSetMenuCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return CreatedAtAction(nameof(GetSetMenuById), new { id = result.Data }, result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateSetMenu(Guid id, [FromBody] UpdateSetMenuCommand command)
        {
            if (id != command.SetMenuId)
            {
                return BadRequest(new { message = "Set menu ID mismatch" });
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPatch("{id}/stock-status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateStockStatus(Guid id, [FromBody] UpdateSetMenuStockStatusCommand command)
        {
            if (id != command.SetMenuId)
            {
                return BadRequest(new { message = "Set menu ID mismatch" });
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(new { success = result.Data });
        }
    }
}
