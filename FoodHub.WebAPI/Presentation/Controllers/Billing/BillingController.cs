using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
using FoodHub.Application.Features.Billing.Commands.CreateQrPayment;
using FoodHub.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using FoodHub.Application.Features.Billing.Commands.SplitBill;
using FoodHub.Application.Features.Billing.Queries.ExportPreCheckBillPdf;
using FoodHub.Application.Features.Billing.Queries.GetBillingHistory;
using FoodHub.Application.Features.Billing.Queries.GetPreCheckBill;
using FoodHub.Application.Features.Billing.Queries.GetRevenueByPaymentMethod;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Billing
{
    [Route("api/v{version:apiVersion}/billing")]
    public class BillingController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public BillingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Xem trước phiếu tạm tính (Pre-check Bill) cho đơn hàng.
        /// </summary>
        /// <remarks>
        /// Không tạo Invoice. Chỉ trả về dữ liệu để hiển thị phiếu tạm tính trên giao diện.
        /// Đơn hàng phải ở trạng thái "Serving".
        /// </remarks>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <response code="200">Trả về thông tin phiếu tạm tính.</response>
        /// <response code="400">Đơn hàng không hợp lệ (sai trạng thái).</response>
        /// <response code="404">Không tìm thấy đơn hàng.</response>
        [HttpGet("orders/{orderId:guid}/pre-check-bill")]
        [HasPermission(Permissions.Billing.PreCheckBill)]
        [ProducesResponseType(typeof(Result<GetPreCheckBillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreCheckBill([FromRoute] Guid orderId)
        {
            var query = new GetPreCheckBillQuery { OrderId = orderId };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Xuất file PDF phiếu tạm tính cho đơn hàng.
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <response code="200">Trả về file PDF.</response>
        /// <response code="400">Đơn hàng không hợp lệ để xuất phiếu tạm tính.</response>
        /// <response code="404">Không tìm thấy đơn hàng.</response>
        [HttpGet("orders/{orderId:guid}/pre-check-bill/pdf")]
        [HasPermission(Permissions.Billing.PreCheckBill)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportPreCheckBillPdf([FromRoute] Guid orderId)
        {
            var query = new ExportPreCheckBillPdfQuery { OrderId = orderId };
            var result = await _mediator.Send(query);
            return HandleFileResult(
                result,
                data => data.Content,
                "application/pdf",
                data => data.FileName
            );
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
        [ProducesResponseType(
            typeof(Result<PagedResult<GetBillingHistoryResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetBillingHistory([FromQuery] PaginationParams pagination)
        {
            var query = new GetBillingHistoryQuery { Pagination = pagination };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Báo cáo doanh thu theo phương thức thanh toán.
        /// </summary>
        /// <param name="dateFrom">Ngày bắt đầu (mặc định: hôm nay).</param>
        /// <param name="dateTo">Ngày kết thúc (mặc định: hôm nay).</param>
        /// <param name="paymentMethodConfigId">Lọc theo phương thức cụ thể (tùy chọn).</param>
        /// <response code="200">Báo cáo doanh thu.</response>
        [HttpGet("revenue-by-payment-method")]
        [HasPermission(Permissions.Billing.ViewHistory)]
        [ProducesResponseType(typeof(Result<GetRevenueByPaymentMethodResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueByPaymentMethod(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] Guid? paymentMethodConfigId)
        {
            var query = new GetRevenueByPaymentMethodQuery
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                PaymentMethodConfigId = paymentMethodConfigId
            };
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
        /// Tách một bill thành bill mới cùng bàn.
        /// </summary>
        [HttpPost("orders/{orderId:guid}/split-bill")]
        [HasPermission(Permissions.Billing.SplitBill)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<SplitBillResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SplitBill(
            [FromRoute] Guid orderId,
            [FromBody] SplitBillCommand command
        )
        {
            command.OrderId = orderId;
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
