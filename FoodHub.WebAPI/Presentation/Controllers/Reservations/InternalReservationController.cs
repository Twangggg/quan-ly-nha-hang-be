using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation;
using FoodHub.Application.Features.Reservations.Commands.UpdateReservation;
using FoodHub.Application.Features.Reservations.Commands.CancelReservation;
using FoodHub.Application.Features.Reservations.Commands.CheckInReservation;
using FoodHub.Application.Features.Reservations.Queries.GetReservations;
using FoodHub.Application.Constants;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
    /// <summary>
    /// Quản lý các đơn đặt bàn (Reservations) dành cho người dùng nội bộ (Quản lý/Thu ngân).
    /// </summary>
    [Tags("Đặt bàn - Nội bộ (Internal Reservations)")]
    // [Authorize(Roles = "Manager, Cashier")] // Giả sử Internal User Roles
    [Authorize]
    [Route("api/v{version:apiVersion}/reservations")]
    public class InternalReservationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InternalReservationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách các đơn đặt bàn với phân trang và bộ lọc.
        /// </summary>
        /// <param name="query">Bộ lọc và tham số phân trang cho danh sách đặt bàn.</param>
        /// <response code="200">Trả về danh sách các đơn đặt bàn theo yêu cầu.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<ReservationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReservations([FromQuery] GetReservationsQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới một đơn đặt bàn từ phía nhân viên nhà hàng (nội bộ).
        /// </summary>
        /// <param name="command">Thông tin chi tiết của đơn đặt bàn cần tạo.</param>
        /// <response code="200">Tạo mới thành công, trả về ID của đơn đặt bàn.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateReservation([FromBody] CreateInternalReservationCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin của một đơn đặt bàn đã tồn tại.
        /// </summary>
        /// <param name="id">Mã định danh của đơn đặt bàn cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật mới cho đơn đặt bàn.</param>
        /// <response code="200">Cập nhật thành công, trả về ID của đơn đặt bàn.</response>
        /// <response code="400">ID không khớp hoặc dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy đơn đặt bàn.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReservation(Guid id, [FromBody] UpdateReservationCommand command)
        {
            command.ReservationId = id; 
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Hủy một đơn đặt bàn đang chờ hoặc đã xác nhận.
        /// </summary>
        /// <param name="id">Mã định danh của đơn đặt bàn cần hủy.</param>
        /// <response code="200">Hủy đặt bàn thành công.</response>
        /// <response code="404">Không tìm thấy đơn đặt bàn.</response>
        /// <response code="400">Trạng thái hiện tại của đơn đặt bàn không cho phép hủy.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            var result = await _mediator.Send(new CancelReservationCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Check-in khách hàng đã đặt bàn. Hệ thống sẽ tự động tạo đơn hàng (Order) phục vụ tại bàn tương ứng.
        /// </summary>
        /// <param name="id">Mã định danh của đơn đặt bàn (ReservationId).</param>
        /// <response code="200">Check-in thành công. Trả về thông tin OrderId và OrderCode.</response>
        /// <response code="400">Yêu cầu không hợp lệ (ví dụ: trạng thái đặt bàn không phù hợp).</response>
        /// <response code="404">Không tìm thấy thông tin đặt bàn.</response>
        /// <response code="409">Xung đột dữ liệu (ví dụ: bàn đang bận hoặc đã có order).</response>
        [HttpPost("{id:guid}/check-in")]
        [HasPermission(Permissions.Reservations.CheckIn)]
        [ProducesResponseType(typeof(Result<CheckInReservationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckIn(Guid id)
        {
            var command = new CheckInReservationCommand { ReservationId = id };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

    }
}
