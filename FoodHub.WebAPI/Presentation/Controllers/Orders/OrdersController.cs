using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.OrderItems.Commands.AddOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity;
using FoodHub.Application.Features.OrderItems.Commands.CancelOrderItem;
using FoodHub.Application.Features.OrderItems.Commands.UpdateOrderItem;
using FoodHub.Application.Features.Orders.Commands.CancelOrder;
using FoodHub.Application.Features.Orders.Commands.CompleteOrder;
using FoodHub.Application.Features.Orders.Commands.CreateOrder;
using FoodHub.Application.Features.Orders.Commands.SubmitOrderToKitchen;
using FoodHub.Application.Features.Orders.Queries.GetOrderAuditLogs;
using FoodHub.Application.Features.Orders.Queries.GetOrderById;
using FoodHub.Application.Features.Orders.Queries.GetOrders;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
    [RateLimit(maxRequests: 1000, windowMinutes: 1, blockMinutes: 5)]
    public class OrdersController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public OrdersController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
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
        [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
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
        /// Lấy đơn hàng dựa trên id
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <response code="200">Trả về đơn hàng kèm thông tin tương ứng.</response>
        [HttpGet("{orderId:guid}")]
        [HasPermission(Permissions.Orders.View)]
        [ProducesResponseType(typeof(Result<GetOrderByIdResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var result = await _mediator.Send(new GetOrderByIdQuery { OrderId = orderId });
            return HandleResult(result);
        }

        /// <summary>
        /// Gửi toàn bộ yêu cầu của đơn hàng xuống bếp.
        /// </summary>
        /// <remarks>Khi nhân viên nhấn "Gửi bếp", trạng thái các món ăn sẽ chuyển sang 'Pending'.</remarks>
        [HttpGet("{orderId:guid}/audit-logs")]
        [HasPermission(Permissions.Orders.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetOrderAuditLogsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetOrderAuditLogs(
            Guid orderId,
            [FromQuery] PaginationParams pagination
        )
        {
            var result = await _mediator.Send(new GetOrderAuditLogsQuery(orderId, pagination));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        [HttpPost("{orderId:guid}/submit-to-kitchen")]
        [HasPermission(Permissions.Orders.SubmitToKitchen)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitOrderToKitchen(
            Guid orderId,
            [FromBody] SubmitOrderToKitchenCommand command
        )
        {
            command.OrderId = orderId;
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
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Điều chỉnh số lượng một món ăn trong đơn hàng.
        /// </summary>
        [HttpPatch("{id:guid}/items/{itemId:guid}/quantity")]
        [HasPermission(Permissions.Orders.Update)]
        [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<AdjustOrderItemQuantityResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdjustOrderItemQuantity(
            Guid id,
            Guid itemId,
            [FromBody] AdjustOrderItemQuantityCommand command
        )
        {
            if (id != command.OrderId)
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            if (itemId != command.OrderItemId)
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );

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
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
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
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            if (itemId != command.OrderItemId)
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );

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
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
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
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
