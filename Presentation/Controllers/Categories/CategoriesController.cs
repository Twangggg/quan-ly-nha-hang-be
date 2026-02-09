using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Categories.Commands.CreateCategory;
using FoodHub.Application.Features.Categories.Commands.DeleteCategory;
using FoodHub.Application.Features.Categories.Commands.UpdateCategory;
using FoodHub.Application.Features.Categories.Queries.GetAllCategories;
using FoodHub.Application.Features.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [Tags("Categories")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var query = new GetAllCategoriesQuery();
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var query = new GetCategoryByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetCategoryById), new { id = result.Data }, result.Data);
            }

            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command)
        {
            if (id != command.CategoryId)
            {
                return BadRequest(new { message = "Category ID mismatch" });
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _mediator.Send(new DeleteCategoryCommand(id));
            return HandleResult(result);
        }
    }
}
