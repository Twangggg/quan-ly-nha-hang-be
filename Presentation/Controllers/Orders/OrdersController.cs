using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
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
                ResultErrorType.Conflict => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }

        [HttpPost("submit-to-kitchen")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> SubmitOrderToKitchen([FromBody] SubmitOrderToKitchenCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
