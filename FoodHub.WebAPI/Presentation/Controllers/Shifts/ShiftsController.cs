using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.CreateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Application.Features.Shifts.Queries.GetShifts;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FoodHub.Application.Features.Shifts.Queries.GetShiftsByEmployeeId;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Request object để cập nhật trạng thái hoạt động của ca làm việc.
    /// </summary>
    public record UpdateShiftStatusRequest(
        [property: JsonPropertyName("isActive")] bool IsActive
    );

    [Tags("Ca làm việc (Shifts)")]
    [HasPermission(Permissions.Shifts.View)]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class ShiftsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public ShiftsController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy danh sách phân trang ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Shifts.View.
        /// Dùng để hiển thị danh sách các ca làm việc trong hệ thống.
        /// </remarks>
        /// <param name="pagination">Tham số phân trang và lọc (PageNumber, PageSize).</param>
        /// <response code="200">Trả về danh sách ca làm việc kèm Header phân trang.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<GetShiftByIdResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShifts([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetShiftsQuery(pagination));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết ca làm việc theo ID.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Shifts.View.
        /// </remarks>
        /// <param name="id">Mã ca làm việc.</param>
        /// <response code="200">Trả về thông tin chi tiết ca làm việc.</response>
        /// <response code="404">Không tìm thấy ca làm việc.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<GetShiftByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetShift(Guid id)
        {
            var result = await _mediator.Send(new GetShiftByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới một ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Shifts.Create.
        /// </remarks>
        /// <param name="command">Thông tin ca làm việc mới.</param>
        /// <response code="200">Đã tạo ca làm việc thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost]
        [HasPermission(Permissions.Shifts.Create)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<CreateShiftResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Shifts.Update.
        /// </remarks>
        /// <param name="id">Mã ca làm việc cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">ID không khớp hoặc dữ liệu không hợp lệ.</response>
        [HttpPut("{id:guid}")]
        [HasPermission(Permissions.Shifts.Update)]
        [ProducesResponseType(typeof(Result<UpdateShiftResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateShift(Guid id, [FromBody] UpdateShiftCommand command)
        {
            if (id != command.ShiftId)
            {
                return BadRequest(new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    _messageService.GetMessage(MessageKeys.Common.IdMismatch)));
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hoạt động của ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Shifts.Update.
        /// </remarks>
        /// <param name="id">Mã ca làm việc.</param>
        /// <param name="request">Trạng thái hoạt động mới.</param>
        /// <response code="200">Cập nhật trạng thái thành công.</response>
        [HttpPatch("{id:guid}/status")]
        [HasPermission(Permissions.Shifts.Update)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShiftStatusRequest request)
        {
            var result = await _mediator.Send(new UpdateShiftStatusCommand(id, request.IsActive));
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách ca làm việc của nhân viên hiện tại.
        /// </summary>
        /// <returns></returns>
        [HttpPatch("myShift")]
        [HasPermission(Permissions.Shifts.ViewMyShifts)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<List<GetShiftsByEmployeeIdResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetShiftsByEmployeeId()
        {
            var result = await _mediator.Send(new GetShiftsByEmployeeIdQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách ca làm việc của nhân viên hiện tại.
        /// </summary>
        /// <returns>Danh sách ca làm việc của nhân viên hiện tại.</returns>
        [HttpGet("myShifts")]
        [HasPermission(Permissions.Shifts.ViewMyShifts)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<List<GetShiftsByEmployeeIdResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetShiftsByEmployeeIdV2()
        {
            var result = await _mediator.Send(new GetShiftsByEmployeeIdQuery());
            return HandleResult(result);
        }
    }
}
