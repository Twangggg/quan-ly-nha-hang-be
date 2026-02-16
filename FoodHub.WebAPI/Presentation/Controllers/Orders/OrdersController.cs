using System.Net.Mime;
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
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các hoạt động liên quan đến Đơn hàng (Orders) và Chi tiết đơn hàng (OrderItems).
    /// </summary>
    [Tags("Đơn hàng (Orders)")]
    [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
    public class OrdersController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo một đơn hàng mới.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Orders.Create.
        /// Dùng khi khách hàng bắt đầu đặt bàn hoặc tạo đơn mới.
        /// </remarks>
        /// <param name="command">Thông tin đơn hàng bao gồm TableId và danh sách món ăn ban đầu.</param>
        /// <response code="200">Đã tạo đơn hàng thành công, trả về mã ID đơn hàng.</response>
        /// <response code="400">Dữ liệu không hợp lệ (ví dụ: bàn đã có người).</response>
        [HttpPost]
        [HasPermission(Permissions.Orders.Create)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách các đơn hàng có phân trang.
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc (PageNumber, PageSize).</param>
        /// <response code="200">Trả về danh sách đơn hàng kèm Header phân trang.</response>
        [HttpGet]
        [HasPermission(Permissions.Orders.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetOrdersResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetOrders([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetOrdersQuery { Pagination = pagination });
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Gửi toàn bộ yêu cầu của đơn hàng xuống bếp.
        /// </summary>
        /// <remarks>Khi nhân viên nhấn "Gửi bếp", trạng thái các món ăn sẽ chuyển sang 'Pending'.</remarks>
        [HttpPost("submit-to-kitchen")]
        [HasPermission(Permissions.Orders.SubmitToKitchen)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitOrderToKitchen(
            [FromBody] SubmitOrderToKitchenCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật số lượng hoặc ghi chú của một món ăn trong đơn hàng.
        /// </summary>
        /// <param name="id">Mã đơn hàng.</param>
        /// <param name="command">Thông tin cập nhật món ăn.</param>
        [HttpPatch("{id:guid}/items")]
        [HasPermission(Permissions.Orders.Update)]
        [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Hủy bỏ toàn bộ đơn hàng.
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hủy.</param>
        /// <param name="command">Lý do hủy đơn hàng.</param>
        [HttpPatch("{id:guid}/cancel")]
        [HasPermission(Permissions.Orders.Cancel)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderCommand command)
        {
            if (id != command.OrderId)
                return BadRequest(new { message = "Order ID mismatch" });
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Hủy bỏ một món ăn cụ thể trong đơn hàng.
        /// </summary>
        [HttpPatch("{id:guid}/items/{itemId:guid}/cancel")]
        [HasPermission(Permissions.Orders.Cancel)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Thêm món ăn mới vào một đơn hàng đang hoạt động.
        /// </summary>
        [HttpPost("{id:guid}/items")]
        [HasPermission(Permissions.Orders.Update)]
        [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Hoàn tất đơn hàng (Thanh toán xong).
        /// </summary>
        [HttpPatch("{id:guid}/complete")]
        [HasPermission(Permissions.Orders.Complete)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
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
