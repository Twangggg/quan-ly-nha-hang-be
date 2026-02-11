using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Application.Features.Orders.Commands.CancelOrder;
using FoodHub.Application.Features.Orders.Commands.CompleteOrder;
using FoodHub.Application.Features.Orders.Commands.CreateOrder;
using FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen;
using FoodHub.Application.Features.Orders.Queries.GetOrders;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Orders")]
    [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
    public class OrdersController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [HasPermission(Permissions.Orders.Create)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet]
        [HasPermission(Permissions.Orders.View)]
        public async Task<IActionResult> GetOrders([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetOrdersQuery { Pagination = pagination });
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        [HttpPost("submit-to-kitchen")]
        [HasPermission(Permissions.Orders.SubmitToKitchen)]
        public async Task<IActionResult> SubmitOrderToKitchen(
            [FromBody] SubmitOrderToKitchenCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/items")]
        [HasPermission(Permissions.Orders.Update)]
        [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> UpdateOrderItem(
            Guid id,
            [FromBody] UpdateOrderItemCommand command
        )
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "Order ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/cancel")]
        [HasPermission(Permissions.Orders.Cancel)]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderCommand command)
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "Order ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/items/{itemId}/cancel")]
        [HasPermission(Permissions.Orders.Cancel)]
        public async Task<IActionResult> CancelOrderItem(
            Guid id,
            Guid itemId,
            [FromBody] CancelOrderItemCommand command
        )
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "Order ID mismatch" });
            if (itemId != command.OrderItemId)
                return BadRequest(new { message = "Item ID mismatch" });

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("{id}/items")]
        [HasPermission(Permissions.Orders.Update)]
        [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> AddOrderItem(
            Guid id,
            [FromBody] AddOrderItemCommand command
        )
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPatch("{id}/complete")]
        [HasPermission(Permissions.Orders.Complete)]
        public async Task<IActionResult> CompleteOrder(
            Guid id,
            [FromBody] CompleteOrderCommand command
        )
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
