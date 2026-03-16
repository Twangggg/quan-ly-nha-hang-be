using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable;
using FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder;
using FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.MergeSplitOrder
{
    [Tags("Đơn hàng (Orders)")]
    [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
    public class TableOperationsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public TableOperationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Đổi bàn cho một đơn hàng. Món ăn và thông tin đơn được chuyển sang bàn mới.</summary>
        [HttpPatch("{id:guid}/change-table")]
        [HasPermission(Permissions.Orders.ChangeTable)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ChangeOrderTableResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeOrderTable(Guid id, [FromBody] ChangeOrderTableCommand command)
        {
            var result = await _mediator.Send(command with { OrderId = id });
            return HandleResult(result);
        }

        /// <summary>Gộp hai đơn hàng thành một. Khách hàng và món được gom sang đơn đích.</summary>
        [HttpPost("{id:guid}/merge")]
        [HasPermission(Permissions.Orders.Merge)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MergeOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MergeOrder(Guid id, [FromBody] MergeOrderCommand command)
        {
            var result = await _mediator.Send(command with { FirstOrder = id });
            return HandleResult(result);
        }

        /// <summary>Tách một đơn hàng thành hai. Một phần món được chuyển sang đơn mới.</summary>
        [HttpPost("{id:guid}/split")]
        [HasPermission(Permissions.Orders.Split)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SplitOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SplitOrder(Guid id, [FromBody] SplitOrderCommand command)
        {
            var result = await _mediator.Send(command with { SourceOrderId = id });
            return HandleResult(result);
        }
    }
}
