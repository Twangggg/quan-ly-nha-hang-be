using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Features.ShiftAssignments.Commands.AutoAssignShift;
using FoodHub.Application.Features.ShiftAssignments.Commands.CancelShiftAssignment;
using FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignmentById;
using FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignments;
using FoodHub.Application.Features.ShiftAssignments.Commands.UpdateShiftAssignment;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces.Common;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý phân công ca làm việc cho nhân viên.
    /// </summary>
    [Tags("Phân công ca (Shift Assignments)")]
    [HasPermission(Permissions.ShiftAssignments.View)]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class ShiftAssignmentsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public ShiftAssignmentsController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy danh sách phân công ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.View.
        /// </remarks>
        /// <param name="query">Tham số phân trang và lọc.</param>
        /// <response code="200">Trả về danh sách phân công ca kèm Header phân trang.</response>
        [HttpGet]
        [HasPermission(Permissions.ShiftAssignments.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetShiftAssignmentsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShiftAssignments([FromQuery] GetShiftAssignmentsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một phân công ca theo ID.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.View.
        /// </remarks>
        /// <param name="id">Mã phân công ca.</param>
        /// <response code="200">Trả về thông tin chi tiết phân công ca.</response>
        /// <response code="404">Không tìm thấy phân công ca.</response>
        [HttpGet("{id:guid}")]
        [HasPermission(Permissions.ShiftAssignments.View)]
        [ProducesResponseType(typeof(Result<GetShiftAssignmentByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetShiftAssignmentByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Gán một ca làm việc cho nhân viên.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.Create.
        /// </remarks>
        /// <param name="command">Thông tin gán ca.</param>
        /// <response code="200">Đã gán ca thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="409">Xung đột lịch làm việc.</response>
        [HttpPost]
        [HasPermission(Permissions.ShiftAssignments.Create)]
        [ProducesResponseType(typeof(Result<AssignShiftResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignShift([FromBody] AssignShiftCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Tự động gán ca làm việc cho nhân viên trong một khoảng thời gian.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.Create.
        /// Các ngày đã có lịch sẽ được bỏ qua.
        /// </remarks>
        /// <param name="command">Thông tin gán ca tự động.</param>
        /// <response code="200">Đã xử lý gán ca tự động thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost("auto")]
        [HasPermission(Permissions.ShiftAssignments.Create)]
        [ProducesResponseType(typeof(Result<List<AssignShiftResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AutoAssignShift([FromBody] AutoAssignShiftCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Hủy phân công ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.Delete.
        /// </remarks>
        /// <param name="id">Mã phân công ca cần hủy.</param>
        /// <response code="200">Hủy thành công.</response>
        /// <response code="404">Không tìm thấy phân công ca.</response>
        [HttpDelete("{id:guid}")]
        [HasPermission(Permissions.ShiftAssignments.Delete)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelShiftAssignment(Guid id)
        {
            var result = await _mediator.Send(new CancelShiftAssignmentCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật phân công ca làm việc.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: ShiftAssignments.Update.
        /// </remarks>
        /// <param name="id">Mã phân công ca cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy phân công ca.</response>
        /// <response code="409">Xung đột lịch làm việc.</response>
        [HttpPut("{id:guid}")]
        [HasPermission(Permissions.ShiftAssignments.Update)]
        [ProducesResponseType(typeof(Result<AssignShiftResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateShiftAssignment(Guid id, [FromBody] UpdateShiftAssignmentCommand command)
        {
            if (id != command.ShiftAssignmentId)
            {
                return BadRequest(new ErrorResponse(StatusCodes.Status400BadRequest, _messageService.GetMessage(MessageKeys.Common.IdMismatch)));
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
