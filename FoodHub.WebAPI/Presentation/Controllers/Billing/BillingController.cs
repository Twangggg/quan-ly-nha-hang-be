using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
using FoodHub.Application.Features.Billing.Queries.GetBillingHistory;
using FoodHub.Application.Interfaces;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using FoodHub.Application.Features.Billing.Commands.CreateQrPayment;
using FoodHub.Application.Features.Billing.Commands.ProcessPaymentWebhook;

namespace FoodHub.WebAPI.Presentation.Controllers.Billing
{
    [Route("api/v{version:apiVersion}/billing")]
    public class BillingController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public BillingController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Thanh toán hóa đơn (Checkout Order).
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <param name="command">Thông tin thanh toán (phương thức, số tiền khách đưa).</param>
        /// <response code="200">Checkout thành công.</response>
        /// <response code="400">Lỗi nghiệp vụ (Không đủ tiền, đơn đã thanh toán...).</response>
        /// <response code="404">Không tìm thấy đơn hàng.</response>
        [HttpPost("orders/{orderId:guid}/checkout")]
        [HasPermission(Permissions.Billing.Checkout)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckoutOrder(
            [FromRoute] Guid orderId,
            [FromBody] CheckoutOrderCommand command
        )
        {
            if (command.OrderId != Guid.Empty && command.OrderId != orderId)
            {
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            }
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy lịch sử giao dịch thanh toán.
        /// </summary>
        /// <param name="pagination">Thông tin phân trang, tìm kiếm, lọc.</param>
        /// <response code="200">Danh sách giao dịch.</response>
        [HttpGet("history")]
        [HasPermission(Permissions.Billing.ViewHistory)]
        [ProducesResponseType(typeof(Result<PagedResult<GetBillingHistoryResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBillingHistory([FromQuery] PaginationParams pagination)
        {
            var query = new GetBillingHistoryQuery { Pagination = pagination };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mã QR thanh toán qua PayOS cho đơn hàng.
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <response code="200">Tạo QR thành công.</response>
        /// <response code="400">Lỗi nghiệp vụ (Không đủ tiền, đơn đã thanh toán...).</response>
        /// <response code="404">Không tìm thấy đơn hàng.</response>
        [HttpPost("orders/{orderId:guid}/payos-qr")]
        [HasPermission(Permissions.Billing.Checkout)]
        [ProducesResponseType(typeof(Result<PaymentLinkResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateQrPayment([FromRoute] Guid orderId)
        {
            var command = new CreateQrPaymentCommand { OrderId = orderId };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Endpoint nhận Webhook từ PayOS.
        /// </summary>
        /// <response code="200">Xử lý Webhook thành công.</response>
        [AllowAnonymous]
        [HttpPost("payos-webhook")]
        public async Task<IActionResult> PayosWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var command = new ProcessPaymentWebhookCommand { WebhookBody = body };
            await _mediator.Send(command);

            return Ok(new { success = true });
        }
    }
}


