using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.CreateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Application.Features.Shifts.Queries.GetShifts;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý thiết lập các ca làm việc trong hệ thống (Master Data).
    /// </summary>
    [Tags("Ca làm việc (Shifts)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class ShiftsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ShiftsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách tất cả các ca làm việc.
        /// </summary>
        /// <returns code="200">Danh sách các ca làm việc.</returns>
        [HttpGet]
        [HasPermission(Permissions.Shifts.View)]
        [ProducesResponseType(typeof(Result<List<GetShiftByIdResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllShifts()
        {
            var result = await _mediator.Send(new GetShiftsQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một ca làm việc theo ID.
        /// </summary>
        /// <param name="id">ID của ca làm việc.</param>
        /// <response code="200">Thông tin ca làm việc.</response>
        /// <response code="404">Không tìm thấy ca làm việc.</response>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Shifts.View)]
        [ProducesResponseType(typeof(Result<GetShiftByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetShiftById(Guid id)
        {
            var result = await _mediator.Send(new GetShiftByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một ca làm việc mới.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Shifts.Create.</remarks>
        /// <param name="command">Dữ liệu tạo ca làm việc.</param>
        /// <response code="201">Ca làm việc đã được tạo thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost]
        [HasPermission(Permissions.Shifts.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<CreateShiftResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleCreated(result, d => $"/api/v1/shifts/{d.ShiftId}");
        }

        /// <summary>
        /// Cập nhật thông tin ca làm việc (Tên, Giờ bắt đầu/kết thúc).
        /// </summary>
        /// <remarks>Yêu cầu quyền: Shifts.Update.</remarks>
        /// <param name="id">ID của ca làm việc cần cập nhật.</param>
        /// <param name="command">Dữ liệu cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy ca làm việc.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Shifts.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<UpdateShiftResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateShift(Guid id, [FromBody] UpdateShiftCommand command)
        {
            if (id != command.ShiftId)
            {
                return BadRequest("ID in path does not match ID in body.");
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động (Kích hoạt/Vô hiệu hóa) của ca làm việc.
        /// </summary>
        /// <param name="id">ID của ca làm việc.</param>
        /// <param name="request">Thông tin trạng thái mới.</param>
        /// <response code="200">Cập nhật trạng thái thành công.</response>
        /// <response code="404">Không tìm thấy ca làm việc.</response>
        [HttpPatch("{id}/status")]
        [HasPermission(Permissions.Shifts.Deactivate)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateShiftStatus(Guid id, [FromBody] UpdateShiftStatusRequest request)
        {
            var result = await _mediator.Send(new UpdateShiftStatusCommand(id, request.IsActive));
            return HandleResult(result);
        }
    }
}
