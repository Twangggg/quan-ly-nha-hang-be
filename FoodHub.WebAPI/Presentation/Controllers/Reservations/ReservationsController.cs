using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CheckInReservation;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
    /// <summary>
    /// Quản lý các hoạt động liên quan đến Đặt bàn (Reservations) — nội bộ.
    /// </summary>
    [Tags("Đặt bàn (Reservations)")]
    [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
    public class ReservationsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ReservationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Check-in khách hàng đã đặt bàn, hệ thống tự động tạo Order DineIn tương ứng.
        /// </summary>
        /// <param name="reservationId">Mã đặt bàn cần check-in.</param>
        /// <response code="200">Check-in thành công, trả về OrderId và OrderCode.</response>
        /// <response code="400">Trạng thái đặt bàn không hợp lệ để check-in.</response>
        /// <response code="404">Không tìm thấy đặt bàn.</response>
        /// <response code="409">Bàn đang bận hoặc đã có Order cho đặt bàn này.</response>
        [HttpPost("{reservationId:guid}/check-in")]
        [HasPermission(Permissions.Reservations.CheckIn)]
        [ProducesResponseType(typeof(Result<CheckInReservationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckIn(Guid reservationId)
        {
            var command = new CheckInReservationCommand { ReservationId = reservationId };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
