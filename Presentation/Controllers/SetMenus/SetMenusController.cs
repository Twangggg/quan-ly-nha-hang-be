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

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [Tags("SetMenus")]
    public class SetMenusController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SetMenusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetSetMenus(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] List<string>? filters = null,
            [FromQuery] string? orderBy = null)
        {
            var query = new GetSetMenusQuery(pageNumber, pageSize, search, filters, orderBy);
            var result = await _mediator.Send(query);
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
        public async Task<IActionResult> CreateSetMenu([FromBody] CreateSetMenuCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetSetMenuById), new { id = result.Data.SetMenuId }, result.Data);
            }

            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        public async Task<IActionResult> UpdateSetMenu(Guid id, [FromBody] UpdateSetMenuCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        [HttpPut("{id}/stock")]
        [Authorize(Roles = nameof(EmployeeRole.Manager))]
        public async Task<IActionResult> UpdateSetMenuStockStatus(Guid id, [FromBody] UpdateSetMenuStockStatusCommand command)
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSetMenu(Guid id)
        {
            var result = await _mediator.Send(new DeleteSetMenuCommand(id));
            return HandleResult(result);
        }
    }
}
