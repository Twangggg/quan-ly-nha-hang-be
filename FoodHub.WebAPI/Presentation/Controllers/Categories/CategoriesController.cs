using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Categories.Commands.CreateCategory;
using FoodHub.Application.Features.Categories.Commands.DeleteCategory;
using FoodHub.Application.Features.Categories.Commands.UpdateCategory;
using FoodHub.Application.Features.Categories.Queries.GetAllCategories;
using FoodHub.Application.Features.Categories.Queries.GetCategoryById;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý các danh mục món ăn (Categories).
    /// </summary>
    [Tags("Danh mục (Categories)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class CategoriesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy tất cả danh mục món ăn với phân trang.
        /// </summary>
        /// <param name="pagination">Tham số phân trang.</param>
        /// <response code="200">Trả về danh sách danh mục.</response>
        [HttpGet]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetAllCategoriesResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetAllCategories([FromQuery] PaginationParams pagination)
        {
            var query = new GetAllCategoriesQuery(pagination);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một danh mục theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục.</param>
        /// <response code="200">Trả về thông tin danh mục.</response>
        /// <response code="404">Không tìm thấy danh mục.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<GetCategoryByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var query = new GetCategoryByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo một danh mục món ăn mới.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Categories.Create.</remarks>
        /// <param name="command">Thông tin danh mục mới.</param>
        /// <response code="201">Tạo danh mục thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost]
        [HasPermission(Permissions.Categories.Create)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetCategoryById),
                    new { id = result.Data },
                    result.Data
                );
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin danh mục món ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Categories.Update.</remarks>
        /// <param name="id">Mã danh mục cần cập nhật.</param>
        /// <param name="command">Thông tin cập nhật.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="400">ID không khớp hoặc dữ liệu không hợp lệ.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Categories.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCategory(
            Guid id,
            [FromBody] UpdateCategoryCommand command
        )
        {
            if (id != command.CategoryId)
            {
                return BadRequest(new { message = "Category ID mismatch" });
            }

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa một danh mục món ăn.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Categories.Delete.</remarks>
        /// <param name="id">Mã danh mục cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        /// <response code="400">Không thể xóa danh mục đang có món ăn.</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Categories.Delete)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 15)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await _mediator.Send(new DeleteCategoryCommand(id));
            return HandleResult(result);
        }
    }
}
