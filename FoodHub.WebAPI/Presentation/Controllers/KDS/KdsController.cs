using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Commands.CompleteCooking;
using FoodHub.Application.Features.KDS.Commands.RejectOrderItem;
using FoodHub.Application.Features.KDS.Commands.ReturnOrderItem;
using FoodHub.Application.Features.KDS.Commands.StartCooking;
using FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;
using FoodHub.Application.Features.KDS.Queries.GetKdsItems;
using FoodHub.Application.Features.KDS.Queries.GetKdsQueue;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.KDS
{
    /// <summary>
    /// Hệ thống màn hình bếp (Kitchen Display System - KDS).
    /// Quản lý việc chuẩn bị món, điều phối lò và hàng đợi tại các Station.
    /// </summary>
    [Authorize]
    [Tags("Kitchen Display System (KDS)")]
    [RateLimit(maxRequests: 200, windowMinutes: 1, blockMinutes: 5)]
    public class KdsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public KdsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách các món đang 'Preparing' hoặc 'Cooking' tại một Station.
        /// </summary>
        /// <param name="station">Tên station (Bếp Âu, Bếp Á, Bar...).</param>
        /// <response code="200">Danh sách món ăn được sắp xếp theo trạng thái và thời gian (FIFO).</response>
        [HttpGet("items/{station}")]
        [HasPermission(Permissions.Kds.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<List<KdsItemResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetKdsItems(string station)
        {
            var result = await _mediator.Send(new GetKdsItemsQuery { Station = station });
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách hàng đợi (chỉ các món 'Preparing') và vị trí xếp hàng.
        /// </summary>
        [HttpGet("queue/{station}")]
        [HasPermission(Permissions.Kds.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<List<KdsQueueResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKdsQueue(string station)
        {
            var result = await _mediator.Send(new GetKdsQueueQuery { Station = station });
            return HandleResult(result);
        }

        /// <summary>
        /// Bắt đầu nấu một món ăn (Chuyển sang trạng thái 'Cooking').
        /// </summary>
        /// <remarks>Giới hạn tối đa 4 món nấu cùng lúc tại một Station (WIP Limit).</remarks>
        [HttpPost("start-cooking")]
        [HasPermission(Permissions.Kds.Manage)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 1)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartCooking([FromBody] StartCookingCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Đánh dấu món ăn đã nấu xong (Chuyển sang trạng thái 'Completed').
        /// </summary>
        /// <remarks>Sẽ tự động kéo (Auto-pull) món tiếp theo trong hàng đợi lên nấu nếu còn trống chỗ.</remarks>
        [HttpPost("complete-cooking")]
        [HasPermission(Permissions.Kds.Manage)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 1)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteCooking([FromBody] CompleteCookingCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Deprecated alias cho client cũ. Hành vi tương đương complete-cooking.
        /// </summary>
        [HttpPost("mark-ready")]
        [HasPermission(Permissions.Kds.Manage)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 1)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkReadyAlias([FromBody] CompleteCookingCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Từ chối món ăn (Hết nguyên liệu, lỗi món...).
        /// </summary>
        /// <remarks>Yêu cầu quyền Manager hoặc Chef và phải cung cấp lý do từ chối.</remarks>
        [HttpPost("reject")]
        [HasPermission(Permissions.Kds.Reject)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject([FromBody] RejectOrderItemCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Đưa món bị từ chối quay trở lại hàng đợi.
        /// </summary>
        /// <remarks>Chỉ dành cho Manager.</remarks>
        [HttpPost("return")]
        [HasPermission(Permissions.Kds.Return)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReturnToQueue([FromBody] ReturnOrderItemCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy lịch sử hoạt động KDS (Audit Log).
        /// </summary>
        [HttpGet("audit-logs")]
        [HasPermission(Permissions.Kds.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<List<GetKdsAuditLogsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? station = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetKdsAuditLogsQuery
            {
                Station = station,
                Action = action,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return HandleResult(result);
        }
    }
}
