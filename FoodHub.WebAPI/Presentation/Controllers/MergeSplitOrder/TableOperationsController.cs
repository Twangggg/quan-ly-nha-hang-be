using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable;
using FoodHub.Application.Features.MergeSplitOrder.Commands.MergeOrder;
using FoodHub.Application.Features.MergeSplitOrder.Commands.SplitOrder;
using FoodHub.Application.Interfaces;
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
        private readonly IMessageService _messageService;

        public TableOperationsController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Đổi bàn cho một đơn hàng (Lúc này đơn hàng & món phải được mang sang bàn mới).
        /// </summary>
        [HttpPatch("{id:guid}/change-table")]
        [HasPermission(Permissions.Orders.ChangeTable)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeOrderTable(Guid id, [FromBody] ChangeOrderTableCommand command)
        {
            var result = await _mediator.Send(command with { OrderId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Gộp hai đơn hàng thành một. Khách hàng từ đơn này được gom vào đơn kia.
        /// </summary>
        [HttpPost("{id:guid}/merge")]
        [HasPermission(Permissions.Orders.Merge)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MergeOrder(Guid id, [FromBody] MergeOrderCommand command)
        {
            var result = await _mediator.Send(command with { FirstOrder = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Tách một đơn hàng thành hai. Một phần món ăn được tách ra thành đơn mới.
        /// </summary>
        [HttpPost("{id:guid}/split")]
        [HasPermission(Permissions.Orders.Split)] // Hoặc cần quyền chuyên biệt
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SplitOrder(Guid id, [FromBody] SplitOrderCommand command)
        {
            var result = await _mediator.Send(command with { SourceOrderId = id });
            return HandleResult(result);
        }
    }
}
