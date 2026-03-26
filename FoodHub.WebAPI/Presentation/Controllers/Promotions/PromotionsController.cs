using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Features.Orders.Commands.UnapplyPromotion;
using FoodHub.Application.Features.Orders.Commands.ApplyPromotion;
using FoodHub.Application.Features.Promotions.Commands.CreatePromotion;
using FoodHub.Application.Features.Promotions.Commands.DeletePromotion;
using FoodHub.Application.Features.Promotions.Commands.UpdatePromotion;
using FoodHub.Application.Features.Promotions.Commands.UpdatePromotionStatus;
using FoodHub.Application.Features.Promotions.Common;
using FoodHub.Application.Features.Promotions.Queries.GetPromotionById;
using FoodHub.Application.Features.Promotions.Queries.GetPromotions;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.WebAPI.Presentation.Controllers.Promotions
{
    /// <summary>
    /// Quản lý các hoạt động liên quan đến Khuyến mãi (Promotions).
    /// </summary>
    [Tags("Khuyến mãi (Promotions)")]
    public class PromotionsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public PromotionsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy danh sách promotion với phân trang và filter.
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.Vouchers.View)]
        [ProducesResponseType(typeof(Result<PagedResult<PromotionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPromotions([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetPromotionsQuery(pagination));
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết một promotion.
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Vouchers.View)]
        [ProducesResponseType(typeof(Result<PromotionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPromotionById(Guid id)
        {
            var result = await _mediator.Send(new GetPromotionByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới promotion.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Vouchers.Create)]
        [ProducesResponseType(typeof(Result<PromotionResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetPromotionById),
                    new { id = result.Data!.PromotionId },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật promotion.
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Vouchers.Update)]
        [ProducesResponseType(typeof(Result<PromotionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePromotion(Guid id, [FromBody] UpdatePromotionCommand command)
        {
            if (id != command.PromotionId)
            {
                return BadRequest(
                    new ErrorResponse(
                        StatusCodes.Status400BadRequest,
                        _messageService.GetMessage(MessageKeys.Common.IdMismatch)
                    )
                );
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xoa promotion.
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Vouchers.Delete)]
        [ProducesResponseType(typeof(Result<DeletePromotionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePromotion(Guid id)
        {
            var result = await _mediator.Send(new DeletePromotionCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Bat hoac tat promotion.
        /// </summary>
        [HttpPatch("{id}/status")]
        [HasPermission(Permissions.Vouchers.UpdateStatus)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePromotionStatus(Guid id, [FromQuery] bool isActive)
        {
            var result = await _mediator.Send(new UpdatePromotionStatusCommand(id, isActive));
            return HandleResult(result);
        }

        /// <summary>
        /// Gop bo promotion khoi order.
        /// </summary>
        [HttpPost("unapply")]
        [HasPermission(Permissions.Vouchers.Unapply)]
        [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnapplyPromotion([FromBody] UnapplyPromotionCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Áp dụng mã khuyến mãi cho một đơn hàng (Order).
        /// </summary>
        [HttpPost("apply")]
        [HasPermission(Permissions.Vouchers.Apply)] // Reusing permission for now
        [ProducesResponseType(typeof(Result<ApplyPromotionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
