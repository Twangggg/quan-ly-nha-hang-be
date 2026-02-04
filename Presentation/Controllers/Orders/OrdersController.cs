using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Orders.Commands.CreateDraftOrder;
using FoodHub.Application.Features.Orders.Commands.UpdateDraftOrder;
using FoodHub.Application.Features.Orders.Commands.CancelOrder;
using FoodHub.Application.Features.Orders.Commands.AddOrderItem;
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

        [HttpPost]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateDraftOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateDraftOrderCommand command)
        {
            if (id != command.OrderId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderCommand command)
        {
            if (id != command.OrderId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id}/items")]
        [Authorize(Roles = "Manager,Waiter")]
        public async Task<IActionResult> AddOrderItem(Guid id, [FromBody] AddOrderItemCommand command)
        {
            if (id != command.OrderId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
