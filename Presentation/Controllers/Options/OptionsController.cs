using FoodHub.Application.Features.Options.Commands.CreateOptionGroup;
using FoodHub.Application.Features.Options.Commands.CreateOptionItem;
using FoodHub.Application.Features.Options.Commands.DeleteOptionGroup;
using FoodHub.Application.Features.Options.Commands.DeleteOptionItem;
using FoodHub.Application.Features.Options.Commands.UpdateOptionGroup;
using FoodHub.Application.Features.Options.Commands.UpdateOptionItem;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/options")]
    [ApiController]
    [Authorize]
    [Tags("Options")]
    public class OptionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OptionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region OptionGroup

        [HttpGet("menu-item/{menuItemId}")]
        public async Task<IActionResult> GetOptionGroupsByMenuItem(Guid menuItemId)
        {
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPost("group")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateOptionGroup([FromBody] CreateOptionGroupCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPut("group/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateOptionGroup(Guid id, [FromBody] UpdateOptionGroupCommand command)
        {
            if (id != command.OptionGroupId)
            {
                return BadRequest(new { message = "Option group ID mismatch" });
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpDelete("group/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteOptionGroup(Guid id)
        {
            var result = await _mediator.Send(new DeleteOptionGroupCommand(id));

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        #endregion

        #region OptionItem

        [HttpPost("item")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateOptionItem([FromBody] CreateOptionItemCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPut("item/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateOptionItem(Guid id, [FromBody] UpdateOptionItemCommand command)
        {
            if (id != command.OptionItemId)
            {
                return BadRequest(new { message = "Option item ID mismatch" });
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpDelete("item/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteOptionItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteOptionItemCommand(id));

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        #endregion
    }
}
