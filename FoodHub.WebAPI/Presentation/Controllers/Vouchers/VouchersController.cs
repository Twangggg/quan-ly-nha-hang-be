using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher;
using FoodHub.Application.Features.Vouchers.Commands.CreateVoucher;
using FoodHub.Application.Features.Vouchers.Commands.DeleteVoucher;
using FoodHub.Application.Features.Vouchers.Commands.UnapplyVoucher;
using FoodHub.Application.Features.Vouchers.Commands.UpdateVoucher;
using FoodHub.Application.Features.Vouchers.Commands.UpdateVoucherActive;
using FoodHub.Application.Features.Vouchers.Queries.GetVoucherById;
using FoodHub.Application.Features.Vouchers.Queries.GetVouchers;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Vouchers
{
    /// <summary>
    /// Quản lý các hoạt động liên quan đến Giảm giá (Vouchers).
    /// </summary>
    [Tags("Mã giảm giá (Vouchers)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class VouchersController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public VouchersController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy tất cả mã giảm giá với phân trang.
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.Vouchers.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetVouchersResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVouchers([FromQuery] PaginationParams pagination)
        {
            var query = new GetVouchersQuery(pagination);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mã giảm giá theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Vouchers.View)]
        [ProducesResponseType(typeof(Result<GetVoucherByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVoucherById(Guid id)
        {
            var query = new GetVoucherByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một mã giảm giá mới.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Vouchers.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<CreateVoucherResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetVoucherById),
                    new { id = result.Data!.VoucherId },
                    result.Data
                );
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật mã giảm giá.
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Vouchers.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<UpdateVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateVoucher(Guid id, [FromBody] UpdateVoucherCommand command)
        {
            var result = await _mediator.Send(command with { VoucherId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động của mã giảm giá.
        /// </summary>
        [HttpPatch("{id}/status")]
        [HasPermission(Permissions.Vouchers.Update)]
        [ProducesResponseType(typeof(Result<UpdateVoucherActiveResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateVoucherActive(Guid id, [FromQuery] bool isActive)
        {
            var result = await _mediator.Send(new UpdateVoucherActiveCommand(id, isActive));
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một mã giảm giá.
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Vouchers.Delete)]
        [ProducesResponseType(typeof(Result<DeleteVoucherResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteVoucher(Guid id)
        {
            var result = await _mediator.Send(new DeleteVoucherCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Áp dụng mã giảm giá cho một đơn hàng (Order).
        /// </summary>
        [HttpPost("apply")]
        [HasPermission(Permissions.Vouchers.Apply)]
        [ProducesResponseType(typeof(Result<ApplyVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Gỡ bỏ mã giảm giá khỏi một đơn hàng (Order).
        /// </summary>
        [HttpPost("unapply")]
        [HasPermission(Permissions.Vouchers.Apply)]
        [ProducesResponseType(typeof(Result<UnapplyVoucherResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnapplyVoucher([FromBody] UnapplyVoucherCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
