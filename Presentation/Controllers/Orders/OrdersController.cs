using FoodHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem;
using FoodHub.Application.Features.Orders.Commands.CompleteOrder;
using FoodHub.Application.Features.Orders.Commands.CancelOrder;
using FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen;

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

        [HttpPatch("{id}/items")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> UpdateOrderItem(Guid id, [FromBody] UpdateOrderItemCommand command)
        {
            if (id != command.OrderId) return BadRequest(new { message = "Order ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderCommand command)
        {
            if (id != command.OrderId) return BadRequest(new { message = "Order ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/items/{itemId}/cancel")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CancelOrderItem(Guid id, Guid itemId, [FromBody] CancelOrderItemCommand command)
        {
            if (id != command.OrderId) return BadRequest(new { message = "Order ID mismatch" });
            if (itemId != command.OrderItemId) return BadRequest(new { message = "Item ID mismatch" });

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id}/items")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> AddOrderItem(Guid id, [FromBody] AddOrderItemCommand command)
        {
            if (id != command.OrderId) return BadRequest(new { message = "ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/complete")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CompleteOrder(Guid id, [FromBody] CompleteOrderCommand command)
        {
            if (id != command.OrderId) return BadRequest(new { message = "ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
