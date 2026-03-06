using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Areas.Commands.CreateArea;
using FoodHub.Application.Features.Areas.Commands.UpdateAreaStatus;
using FoodHub.Application.Features.Areas.Commands.UpdateArea;
using FoodHub.Application.Features.Areas.Queries.GetAllAreas;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Application.Interfaces;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các khu vực (Areas).
    /// </summary>
    [Tags("Khu vực (Areas)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class AreasController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public AreasController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy danh sách tất cả các khu vực trong hệ thống.
        /// </summary>
        /// <returns code="200">Danh sách các khu vực.</returns>
        [HttpGet]
        [HasPermission(Permissions.Areas.View)]
        [ProducesResponseType(
            typeof(Result<List<GetAllAreasResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetAllAreas()
        {
            var query = new GetAllAreasQuery();
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }


        /// <summary>
        /// Lấy thông tin khu vực theo ID.
        /// </summary>
        /// <param name="id">ID của khu vực.</param>
        /// <response code="200">Thông tin khu vực.</response>
        /// <response code="404">Không tìm thấy khu vực.</response>
        [HttpGet("{id}")]
        [HasPermission(Permissions.Areas.View)]
        [ProducesResponseType(typeof(Result<GetAreaByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAreaById(Guid id)
        {
            var query = new GetAreaByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo khu vực mới.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Areas.Create.</remarks>
        /// <param name="command">Dữ liệu tạo khu vực.</param>
        /// <response code="201">Khu vực đã được tạo.</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc mã khu vực đã tồn tại.</response>
        [HttpPost]
        [HasPermission(Permissions.Areas.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<GetAreaByIdResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateArea([FromBody] CreateAreaCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess && result.Data != null)
            {
                return CreatedAtAction(
                    nameof(GetAreaById),
                    new { id = result.Data.AreaId },
                    result
                );
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Chỉnh sửa khu vực (Tên, Mô tả). Mã khu vực không được sửa.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Areas.Update.</remarks>
        /// <param name="id">ID của khu vực cần chỉnh sửa.</param>
        /// <param name="command">Dữ liệu cập nhật khu vực.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">ID không khớp hoặc dữ liệu không hợp lệ.</response>
        /// <response code="404">Không tìm thấy khu vực.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Areas.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<GetAreaByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateArea(
            Guid id,
            [FromBody] UpdateAreaCommand command)
        {
            if (id != command.AreaId)
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

        public class UpdateAreaStatusRequest
        {
            [JsonPropertyName("isActive")]
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Thay đổi trạng thái hoạt động của khu vực.
        /// </summary>
        /// <param name="id">Mã khu vực.</param>
        /// <param name="request">Dữ liệu trạng thái mới.</param>
        [HttpPatch("{id}/status")]
        [HasPermission(Permissions.Areas.Update)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAreaStatus(Guid id, [FromBody] UpdateAreaStatusRequest request)
        {
            var result = await _mediator.Send(new UpdateAreaStatusCommand(id, request.IsActive));
            return HandleResult(result);
        }

    }
}
