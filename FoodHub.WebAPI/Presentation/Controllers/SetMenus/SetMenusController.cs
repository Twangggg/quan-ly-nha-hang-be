using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenus;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các Combo/Set ăn (Set Menus).
    /// </summary>
    [Tags("Set ăn (Set Menus)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class SetMenusController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SetMenusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách Set ăn với phân trang.
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc.</param>
        /// <response code="200">Trả về danh sách Set ăn.</response>
        [HttpGet]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetSetMenusResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetSetMenus([FromQuery] PaginationParams pagination)
        {
            var query = new GetSetMenusQuery { Pagination = pagination };
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một Set ăn theo ID.
        /// </summary>
        /// <param name="id">Mã định danh Set ăn.</param>
        /// <response code="200">Trả về thông tin Set ăn.</response>
        /// <response code="404">Không tìm thấy Set ăn.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<GetSetMenuByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSetMenuById(Guid id)
        {
            var query = new GetSetMenuByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một Set ăn mới.
        /// </summary>
        /// <remarks>Yêu cầu quyền: SetMenus.Create.</remarks>
        /// <param name="command">Thông tin Set ăn và danh sách món đi kèm.</param>
        /// <response code="201">Tạo thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost]
        [HasPermission(Permissions.SetMenus.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<CreateSetMenuResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSetMenu([FromBody] CreateSetMenuCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetSetMenuById),
                    new { id = result.Data!.SetMenuId },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin Set ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: SetMenus.Update.</remarks>
        /// <param name="id">Mã Set ăn cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.SetMenus.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSetMenu(
            Guid id,
            [FromBody] UpdateSetMenuCommand command
        )
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hết hàng/còn hàng của Set ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: SetMenus.UpdateStock.</remarks>
        /// <param name="id">Mã Set ăn.</param>
        /// <param name="command">Trạng thái kho hàng mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [HttpPut("{id}/stock")]
        [HasPermission(Permissions.SetMenus.UpdateStock)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSetMenuStockStatus(
            Guid id,
            [FromBody] UpdateSetMenuStockStatusCommand command
        )
        {
            var result = await _mediator.Send(command with { SetMenuId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một Set ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: SetMenus.Delete.</remarks>
        /// <param name="id">Mã Set ăn cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.SetMenus.Delete)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSetMenu(Guid id)
        {
            var result = await _mediator.Send(new DeleteSetMenuCommand(id));
            return HandleResult(result);
        }
    }
}
