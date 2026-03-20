using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.CreateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShift;
using FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Application.Features.Shifts.Queries.GetShifts;
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
    /// Request object để cập nhật trạng thái hoạt động của ca làm việc.
    /// </summary>
    public record UpdateShiftStatusRequest(
        [property: JsonPropertyName("isActive")] bool IsActive
    );
 
    [Tags("Ca làm việc (Shifts)")]
    [Route("api/v1/shifts")]
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
        [HttpGet]
        [HasPermission(Permissions.Shifts.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetShiftByIdResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetShifts([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetShiftsQuery(pagination));
            if (result.IsSuccess && result.Data != null)
                Response.AddPaginationHeaders(result.Data);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết ca làm việc theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Shifts.View)]
        [ProducesResponseType(typeof(Result<GetShiftByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetShift(Guid id)
        {
            return HandleResult(await _mediator.Send(new GetShiftByIdQuery(id)));
        }
 
        /// <summary>
        /// Tạo mới một ca làm việc.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Shifts.Create)]
        [RateLimit(maxRequests: 10, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<CreateShiftResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess) return HandleResult(result);
            return CreatedAtAction(nameof(GetShift), new { id = result.Data!.ShiftId }, result.Data);
        }
 
        /// <summary>
        /// Cập nhật thông tin ca làm việc.
        /// </summary>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Shifts.Update)]
        [ProducesResponseType(typeof(Result<UpdateShiftResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateShift(Guid id, [FromBody] UpdateShiftCommand command)
        {
            if (id != command.ShiftId)
                return BadRequest(new ErrorResponse(StatusCodes.Status400BadRequest, _messageService.GetMessage(MessageKeys.Common.IdMismatch)));
            
            return HandleResult(await _mediator.Send(command));
        }
 
        /// <summary>
        /// Cập nhật trạng thái hoạt động của ca làm việc.
        /// </summary>
        [HttpPatch("{id}/status")]
        [HasPermission(Permissions.Shifts.Update)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShiftStatusRequest request)
        {
            return HandleResult(await _mediator.Send(new UpdateShiftStatusCommand(id, request.IsActive)));
        }
    }
}
