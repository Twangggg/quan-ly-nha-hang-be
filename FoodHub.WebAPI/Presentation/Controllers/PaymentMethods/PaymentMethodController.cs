using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.PaymentMethods.Commands.CreatePaymentMethod;
using FoodHub.Application.Features.PaymentMethods.Commands.TogglePaymentMethodStatus;

using FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethodById;
using FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethods;
using FoodHub.Application.Features.PaymentMethods.Commands.SyncPayOsKeys;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.PaymentMethods
{
    [Route("api/v{version:apiVersion}/payment-methods")]
    public class PaymentMethodController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentMethodController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách phương thức thanh toán.
        /// </summary>
        /// <param name="activeOnly">Nếu true, chỉ trả về phương thức đang Bật.</param>
        /// <response code="200">Danh sách phương thức thanh toán.</response>
        [HttpGet]
        [HasPermission(Permissions.PaymentMethods.View)]
        [ProducesResponseType(typeof(Result<List<GetPaymentMethodsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly)
        {
            var query = new GetPaymentMethodsQuery { ActiveOnly = activeOnly };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết phương thức thanh toán theo ID.
        /// </summary>
        /// <param name="id">ID phương thức thanh toán.</param>
        /// <response code="200">Chi tiết phương thức.</response>
        /// <response code="404">Không tìm thấy.</response>
        [HttpGet("{id:guid}")]
        [HasPermission(Permissions.PaymentMethods.View)]
        [ProducesResponseType(typeof(Result<GetPaymentMethodByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var query = new GetPaymentMethodByIdQuery { PaymentMethodConfigId = id };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo phương thức thanh toán mới.
        /// </summary>
        /// <param name="command">Thông tin phương thức thanh toán.</param>
        /// <response code="200">Tạo thành công.</response>
        /// <response code="400">Lỗi validation hoặc tên trùng.</response>
        [HttpPost]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<CreatePaymentMethodResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentMethodCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }



        /// <summary>
        /// Bật/Tắt phương thức thanh toán.
        /// </summary>
        /// <param name="id">ID phương thức thanh toán.</param>
        /// <response code="200">Thay đổi trạng thái thành công. Trả về trạng thái mới (true = Bật).</response>
        /// <response code="404">Không tìm thấy.</response>
        /// <response code="400">Không thể tắt phương thức mặc định.</response>
        [HttpPatch("{id:guid}/toggle-status")]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleStatus([FromRoute] Guid id)
        {
            var command = new TogglePaymentMethodStatusCommand { PaymentMethodConfigId = id };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Đồng bộ Key từ PayOS Auto-Config Extension.
        /// </summary>
        /// <param name="command">Thông tin 3 Key từ PayOS.</param>
        /// <response code="200">Đồng bộ thành công.</response>
        /// <response code="404">Chưa có phương thức thanh toán chuyển khoản đang Bật.</response>
        [HttpPost("payos-sync")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncPayOsKeys([FromBody] SyncPayOsKeysCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
