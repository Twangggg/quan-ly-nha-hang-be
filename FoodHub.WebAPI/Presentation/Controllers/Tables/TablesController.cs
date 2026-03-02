using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Tables.Commands.CreateTable;
using FoodHub.Application.Features.Tables.Commands.DeleteTable;
using FoodHub.Application.Features.Tables.Commands.UpdateTable;
using FoodHub.Application.Features.Tables.Commands.UpdateTableStatus;
using FoodHub.Application.Features.Tables.Queries.GetTableById;
using FoodHub.Application.Features.Tables.Queries.GetTables;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Tables
{
    /// <summary>
    /// Quản lý các bàn ăn trong nhà hàng (Tables).
    /// </summary>
    [Tags("Bàn ăn (Tables)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class TablesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public TablesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách các bàn ăn với phân trang.
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc.</param>
        /// <returns code="200">Trả về danh sách bàn ăn.</returns>
        [HttpGet(Name = "GetTables")]
        [ProducesResponseType(typeof(Result<PagedResult<GetTablesResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTables([FromQuery] PaginationParams pagination)
        {
            var query = new GetTablesQuery(pagination);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một bàn ăn theo ID.
        /// </summary>
        /// <param name="tableId">ID của bàn ăn.</param>
        /// <returns code="200">Trả về thông tin chi tiết của bàn ăn.</returns>
        /// <returns code="404">Trả về lỗi không tìm thấy bàn ăn.</returns>
        [HttpGet("{tableId}")]
        [ProducesResponseType(typeof(Result<GetTableByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTableById(Guid tableId)
        {
            var query = new GetTableByIdQuery(tableId);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới một bàn ăn trong nhà hàng.
        /// </summary>
        /// <param name="request">Thông tin bàn ăn mới.</param>
        /// <returns code="200">Trả về thông tin của bàn ăn vừa được tạo.</returns>
        /// <returns code="400">Trả về lỗi thông tin bàn ăn không hợp lệ.</returns>
        [HttpPost]
        [HasPermission(Permissions.Tables.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<CreateTableResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTable([FromForm] CreateTableCommand request)
        {
            var result = await _mediator.Send(request);

            if (result.IsSuccess && result.Data != null)
            {
                return CreatedAtAction(
                    nameof(GetTableById),
                    new { tableId = result.Data.TableId },
                    result);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin của một bàn ăn đã tồn tại.
        /// </summary>
        /// <param name="tableId">ID của bàn ăn.</param>
        /// <param name="request">Thông tin bàn ăn cần cập nhật.</param>
        /// <returns code="200">Trả về thông tin của bàn ăn đã được cập nhật.</returns>
        /// <returns code="404">Trả về lỗi không tìm thấy bàn ăn.</returns>
        [HttpPut("{tableId}")]
        [HasPermission(Permissions.Tables.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<UpdateTableResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTable(Guid tableId, [FromForm] UpdateTableCommand request)
        {
            var result = await _mediator.Send(request with { TableId = tableId });
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái của một bàn ăn đã tồn tại (ví dụ: trống, đang sử dụng, đã đặt trước).
        /// </summary>
        /// <param name="tableId">ID của bàn ăn.</param>
        /// <param name="request">Thông tin trạng thái bàn ăn cần cập nhật.</param>
        /// <returns code="200">Trả về thông tin của bàn ăn đã được cập nhật.</returns>
        /// <returns code="404">Trả về lỗi không tìm thấy bàn ăn.</returns>
        [HttpPatch("{tableId}/status")]
        [HasPermission(Permissions.Tables.UpdateStatus)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<UpdateTableStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTableStatus(Guid tableId, [FromBody]UpdateTableStatusCommand request)
        {
            var result = await _mediator.Send(request with { TableId = tableId });
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một bàn ăn khỏi hệ thống. Hành động này sẽ đánh dấu bàn ăn là đã xóa (soft delete) thay vì xóa hoàn toàn khỏi cơ sở dữ liệu.
        /// </summary>
        /// <param name="tableId">ID của bàn ăn.</param>
        /// <returns code="200">Trả về thông tin của bàn ăn đã được xóa.</returns>
        /// <returns code="404">Trả về lỗi không tìm thấy bàn ăn.</returns>
        [HttpDelete("{tableId}")]
        [HasPermission(Permissions.Tables.Delete)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<DeleteTableResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTable(Guid tableId)
        {
            var command = new DeleteTableCommand(tableId);
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
