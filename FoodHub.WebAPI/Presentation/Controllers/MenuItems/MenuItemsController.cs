using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem;
using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock; // Kept this as it was in original, but verified UpdateMenuItemStockStatusCommand usage in added methods
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById;
using FoodHub.Application.Features.MenuItems.Queries.GetMenuItems;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các món ăn trong thực đơn (Menu Items).
    /// </summary>
    [Tags("Món ăn (Menu Items)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class MenuItemsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public MenuItemsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách món ăn với phân trang và lọc.
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc.</param>
        /// <response code="200">Trả về danh sách món ăn.</response>
        [HttpGet(Name = "GetMenuItems")]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetMenuItemsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetMenuItems([FromQuery] PaginationParams pagination)
        {
            var query = new GetMenuItemsQuery { Pagination = pagination };
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một món ăn theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của món ăn.</param>
        /// <response code="200">Trả về thông tin món ăn.</response>
        /// <response code="404">Không tìm thấy món ăn.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<GetMenuItemByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMenuItemById(Guid id)
        {
            var query = new GetMenuItemByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một món ăn mới.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: MenuItems.Create.
        /// Sử dụng [FromForm] để hỗ trợ upload hình ảnh.
        /// </remarks>
        /// <param name="command">Thông tin món ăn mới (bao gồm cả file ảnh).</param>
        /// <response code="201">Tạo món ăn thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost]
        [HasPermission(Permissions.MenuItems.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<CreateMenuItemResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMenuItem([FromForm] CreateMenuItemCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetMenuItemById),
                    new { id = result.Data!.MenuItemId },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin món ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.Update.</remarks>
        /// <param name="id">Mã món ăn cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="404">Không tìm thấy món ăn.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.MenuItems.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMenuItem(Guid id, UpdateMenuItemCommand command)
        {
            var result = await _mediator.Send(command with { MenuItemId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật trạng thái hết hàng/còn hàng của món ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateStock.</remarks>
        /// <param name="id">Mã món ăn.</param>
        /// <param name="command">Trạng thái kho hàng mới.</param>
        /// <response code="200">Cập nhật trạng thái thành công.</response>
        [HttpPut("{id}/stock")]
        [HasPermission(Permissions.MenuItems.UpdateStock)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMenuItemStockStatus(
            Guid id,
            UpdateMenuItemStockStatusCommand command
        )
        {
            var result = await _mediator.Send(command with { MenuItemId = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một món ăn khỏi thực đơn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.Delete.</remarks>
        /// <param name="id">Mã món ăn cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        /// <response code="400">Không thể xóa món ăn đang có trong đơn hàng.</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.MenuItems.Delete)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteMenuItemCommand(id));
            return HandleResult(result);
        }
    }
}
