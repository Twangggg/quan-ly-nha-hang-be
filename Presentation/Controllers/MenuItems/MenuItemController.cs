using FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.MenuItems
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class MenuItemController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MenuItemController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems(
            [FromQuery] string? searchCode,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] Guid? categoryId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetMenuItemsQuery(searchCode, minPrice, maxPrice, categoryId, pageNumber, pageSize);
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
        public async Task<IActionResult> CreateMenuItem([FromBody] CreateMenuItemCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return CreatedAtAction(nameof(GetMenuItemById), new { id = result.Data }, result.Data);
        }

        //[HttpPut("{id}")]
        //[Authorize(Roles = "Manager")]
        //public async Task<IActionResult> UpdateMenuItem(Guid id, [FromBody] UpdateMenuItemCommand command)
        //{
        //    if (id != command.MenuItemId)
        //    {
        //        return BadRequest(new { message = "Menu item ID mismatch" });
        //    }

        //    var result = await _mediator.Send(command);

        //    if (!result.IsSuccess)
        //    {
        //        return BadRequest(new { message = result.Error });
        //    }

        //    return Ok(result.Data);
        //}

        [HttpPatch("{id}/stock-status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateStockStatus(Guid id, [FromBody] UpdateStockStatusCommand command)
        {
            if (id != command.MenuItemId)
            {
                return BadRequest(new { message = "Menu item ID mismatch" });
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
