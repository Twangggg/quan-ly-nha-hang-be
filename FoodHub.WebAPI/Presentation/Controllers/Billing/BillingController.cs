using FoodHub.Application.Constants;
using FoodHub.Application.Features.Billing.Commands.CheckoutOrder;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
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
        /// Thanh toán hóa đơn (Checkout Order).
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <param name="command">Thông tin thanh toán (phương thức, số tiền khách đưa).</param>
        /// <response code="200">Checkout thành công.</response>
        /// <response code="400">Lỗi nghiệp vụ (Không đủ tiền, đơn đã thanh toán...).</response>
        /// <response code="404">Không tìm thấy đơn hàng.</response>
        [HttpPost("orders/{orderId:guid}/checkout")]
        [HasPermission(Permissions.Billing.Checkout)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckoutOrder([FromRoute] Guid orderId, [FromBody] CheckoutOrderCommand command)
        {
            if (command.OrderId != Guid.Empty && command.OrderId != orderId)
            {
                return BadRequest("OrderId trong URL không khớp với body.");
            }
            command.OrderId = orderId;
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
