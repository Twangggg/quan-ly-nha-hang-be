using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Categories.Commands.CreateCategory;
using FoodHub.Application.Features.Categories.Commands.DeleteCategory;
using FoodHub.Application.Features.Categories.Commands.UpdateCategory;
using FoodHub.Application.Features.Categories.Queries.GetAllCategories;
using FoodHub.Application.Features.Categories.Queries.GetCategoryById;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Categories")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class CategoriesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories([FromQuery] PaginationParams pagination)
        {
            var query = new GetAllCategoriesQuery(pagination);
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
        [HasPermission(Permissions.Categories.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetCategoryById),
                    new { id = result.Data },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.Categories.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> UpdateCategory(
            Guid id,
            [FromBody] UpdateCategoryCommand command
        )
        {
            if (id != command.CategoryId)
            {
                return BadRequest(new { message = "Category ID mismatch" });
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Categories.Delete)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _mediator.Send(new DeleteCategoryCommand(id));
            return HandleResult(result);
        }
    }
}
