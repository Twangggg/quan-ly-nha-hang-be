using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShiftRange;
using FoodHub.Application.Features.ShiftAssignments.Commands.CancelShiftAssignment;
using FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignmentById;
using FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignments;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Interfaces;
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
    [Route("api/v1/shift-assignments")]
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
        [HttpGet]
        [HasPermission(Permissions.ShiftAssignments.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetShiftAssignmentsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShiftAssignments([FromQuery] GetShiftAssignmentsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
                Response.AddPaginationHeaders(result.Data);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một phân công ca theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission(Permissions.ShiftAssignments.View)]
        [ProducesResponseType(typeof(Result<GetShiftAssignmentByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return HandleResult(await _mediator.Send(new GetShiftAssignmentByIdQuery(id)));
        }

        /// <summary>
        /// Gán một ca làm việc cho nhân viên.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.ShiftAssignments.Create)]
        [ProducesResponseType(typeof(Result<AssignShiftResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignShift([FromBody] AssignShiftCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess) return HandleResult(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.ShiftAssignmentId }, result);
        }

        /// <summary>
        /// Gán một ca làm việc cho nhân viên theo khoảng ngày.
        /// </summary>
        [HttpPost("range")]
        [HasPermission(Permissions.ShiftAssignments.Create)]
        [ProducesResponseType(typeof(Result<IEnumerable<AssignShiftResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignShiftRange([FromBody] AssignShiftRangeCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess) return HandleResult(result);
            return Ok(result);
        }

        /// <summary>
        /// Hủy phân công ca làm việc.
        /// </summary>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.ShiftAssignments.Delete)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelShiftAssignment(Guid id)
        {
            return HandleResult(await _mediator.Send(new CancelShiftAssignmentCommand(id)));
        }
    }
}
