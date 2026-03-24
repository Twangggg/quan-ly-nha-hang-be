using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Orders.Commands.ApplyPromotion;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Promotions
{
    /// <summary>
    /// Quản lý các hoạt động liên quan đến Khuyến mãi (Promotions).
    /// </summary>
    [Tags("Khuyến mãi (Promotions)")]
    [Route("api/v1/promotions")]
    public class PromotionsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PromotionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Áp dụng mã khuyến mãi cho một đơn hàng (Order).
        /// </summary>
        [HttpPost("apply")]
        [HasPermission(Permissions.Vouchers.Apply)] // Reusing permission for now
        [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
