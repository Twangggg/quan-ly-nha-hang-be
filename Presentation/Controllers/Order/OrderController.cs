using FoodHub.Application.Common.Models;

using FoodHub.Application.Features.Order.Commands.SubmitOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodHub.Application.Features.Orders.Commands.AddOrderItem;
using FoodHub.Application.Features.Orders.Commands.CreateDraftOrder;

namespace FoodHub.Presentation.Controllers.Order
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;
        public OrderController(IMediator mediator)
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
                _ => BadRequest(new { message = result.Error })
            };
        }

        [HttpPost("draft")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CreateDraftOrder([FromBody] CreateDraftOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("add-order-item")]
        [Authorize(Roles ="Manager,Waiter")]
        public async Task<IActionResult> AddOrderItem([FromBody] AddOrderItemCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> SubmitOrder(Guid id)
        {
            var command = new SubmitOrderCommand { OrderId = id };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }

}
