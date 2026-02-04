using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Order.Commands.CreateDraftOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Order
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private IMediator mediator;
        public OrderController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Data);

            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { message = result.Error }),
                ResultErrorType.Unauthorized => Unauthorized(new { message = result.Error }),
                ResultErrorType.Forbidden => Forbid(),
                _ => BadRequest(new { message = result.Error })
            };
        }

        [HttpPost("draft")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CreateDraftOrder([FromBody] CreateDraftOrderCommand command)
        {
            var result = await mediator.Send(command);
            return HandleResult(result);
        }
    }
}
