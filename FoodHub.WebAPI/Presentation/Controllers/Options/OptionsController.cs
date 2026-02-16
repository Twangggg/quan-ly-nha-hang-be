using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Options.Commands.CreateOptionGroup;
using FoodHub.Application.Features.Options.Commands.CreateOptionItem;
using FoodHub.Application.Features.Options.Commands.DeleteOptionGroup;
using FoodHub.Application.Features.Options.Commands.DeleteOptionItem;
using FoodHub.Application.Features.Options.Commands.UpdateOptionGroup;
using FoodHub.Application.Features.Options.Commands.UpdateOptionItem;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các tùy chọn của món ăn (Size, Topping, Ghi chú đi kèm).
    /// </summary>
    [Tags("Tùy chọn (Options)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class OptionsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public OptionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region OptionGroup

        /// <summary>
        /// Lấy danh sách nhóm tùy chọn theo MenuItem ID.
        /// </summary>
        /// <param name="menuItemId">Mã món ăn.</param>
        /// <response code="200">Trả về danh sách nhóm tùy chọn.</response>
        [HttpGet("menu-item/{menuItemId}")]
        [ProducesResponseType(typeof(Result<List<OptionGroupResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOptionGroupsByMenuItem(Guid menuItemId)
        {
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một nhóm tùy chọn mới (ví dụ: Size, Toppings).
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="command">Thông tin nhóm tùy chọn.</param>
        /// <response code="200">Tạo thành công.</response>
        [HttpPost("group")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOptionGroup(
            [FromBody] CreateOptionGroupCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin nhóm tùy chọn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="id">Mã nhóm tùy chọn cần cập nhật.</param>
        /// <param name="command">Thông tin mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [HttpPut("group/{id}")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateOptionGroup(
            Guid id,
            [FromBody] UpdateOptionGroupCommand command
        )
        {
            if (id != command.OptionGroupId)
            {
                return BadRequest(new { message = "Option group ID mismatch" });
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một nhóm tùy chọn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="id">Mã nhóm cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        [HttpDelete("group/{id}")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteOptionGroup(Guid id)
        {
            var result = await _mediator.Send(new DeleteOptionGroupCommand(id));
            return HandleResult(result);
        }

        #endregion

        #region OptionItem

        /// <summary>
        /// Thêm một lựa chọn cụ thể vào nhóm (ví dụ: Thêm 'Size L' vào nhóm 'Size').
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="command">Thông tin lựa chọn.</param>
        /// <response code="200">Tạo thành công.</response>
        [HttpPost("item")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOptionItem(
            [FromBody] CreateOptionItemCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật giá hoặc tên của một lựa chọn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="id">Mã lựa chọn cần cập nhật.</param>
        /// <param name="command">Thông tin mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [HttpPut("item/{id}")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateOptionItem(
            Guid id,
            [FromBody] UpdateOptionItemCommand command
        )
        {
            if (id != command.OptionItemId)
            {
                return BadRequest(new { message = "Option item ID mismatch" });
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một lựa chọn cụ thể.
        /// </summary>
        /// <remarks>Yêu cầu quyền: MenuItems.UpdateOptions.</remarks>
        /// <param name="id">Mã lựa chọn cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        [HttpDelete("item/{id}")]
        [HasPermission(Permissions.MenuItems.UpdateOptions)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteOptionItem(Guid id)
        {
            var result = await _mediator.Send(new DeleteOptionItemCommand(id));
            return HandleResult(result);
        }

        #endregion
    }
}
