using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Employees.Commands.ChangeRole;
using FoodHub.Application.Features.Employees.Commands.CreateEmployee;
using FoodHub.Application.Features.Employees.Commands.DeleteEmployee;
using FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword;
using FoodHub.Application.Features.Employees.Commands.UpdateEmployee;
using FoodHub.Application.Features.Employees.Queries.GetAuditLogs;
using FoodHub.Application.Features.Employees.Queries.GetEmployeeById;
using FoodHub.Application.Features.Employees.Queries.GetEmployees;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý nhân viên (Employees) và phân quyền.
    /// </summary>
    [HasPermission(Permissions.Employees.View)]
    [Tags("Nhân viên (Employees)")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class EmployeesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách nhân viên với phân trang.
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc.</param>
        /// <response code="200">Trả về danh sách nhân viên.</response>
        [HttpGet]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetEmployeesResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetEmployeesAsync([FromQuery] PaginationParams pagination)
        {
            var query = new GetEmployeesQuery(pagination);
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult<PagedResult<GetEmployeesResponse>>(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết nhân viên theo ID.
        /// </summary>
        /// <param name="id">Mã định danh nhân viên.</param>
        /// <response code="200">Trả về thông tin nhân viên.</response>
        /// <response code="404">Không tìm thấy nhân viên.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<GetEmployeesResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeById(Guid id)
        {
            var query = new GetEmployeeByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo tài khoản nhân viên mới.
        /// </summary>
        /// <remarks>
        /// Yêu cầu quyền: Employees.Create.
        /// Mật khẩu mặc định sẽ được sinh ra hoặc gửi qua email tùy cấu hình.
        /// </remarks>
        /// <param name="command">Thông tin nhân viên mới.</param>
        /// <response code="201">Tạo thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc Email/Mã nhân viên đã tồn tại.</response>
        [HttpPost]
        [HasPermission(Permissions.Employees.Create)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<GetEmployeesResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEmployeeAsync(
            [FromBody] CreateEmployeeCommand command
        )
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
                return HandleResult(result);
            return CreatedAtAction(
                nameof(GetEmployeeById),
                new { id = result.Data!.EmployeeId },
                result.Data
            );
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân của nhân viên.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Employees.Update.</remarks>
        /// <param name="id">Mã nhân viên cần cập nhật.</param>
        /// <param name="command">Thông tin mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [HttpPut("{id}")]
        [HasPermission(Permissions.Employees.Update)]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEmployeeAsync(
            Guid id,
            [FromBody] UpdateEmployeeCommand command
        )
        {
            if (id != command.EmployeeId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xóa tài khoản nhân viên (Soft Delete).
        /// </summary>
        /// <remarks>Yêu cầu quyền: Employees.Delete.</remarks>
        /// <param name="id">Mã nhân viên cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        [HttpDelete("{id}")]
        [HasPermission(Permissions.Employees.Delete)]
        [RateLimit(maxRequests: 10, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteEmployeeAsync(Guid id)
        {
            var result = await _mediator.Send(new DeleteEmployeeCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy lịch sử hoạt động (Audit Logs) của nhân viên.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Employees.ViewAuditLogs.</remarks>
        /// <param name="id">Mã nhân viên.</param>
        /// <param name="pagination">Tham số phân trang.</param>
        /// <response code="200">Trả về danh sách logs.</response>
        [HttpGet("{id}/audit-logs")]
        [HasPermission(Permissions.Employees.ViewAuditLogs)]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetAuditLogsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] PaginationParams pagination
        )
        {
            var query = new GetAuditLogsQuery(id, pagination);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult<PagedResult<GetAuditLogsResponse>>(result);
        }

        /// <summary>
        /// Thay đổi quyền hạn/vai trò của nhân viên.
        /// </summary>
        /// <remarks>Yêu cầu quyền: Employees.ChangeRole.</remarks>
        /// <param name="command">Thông tin chuyển đổi vai trò.</param>
        /// <response code="200">Thay đổi thành công.</response>
        [HttpPost("change-role")]
        [HasPermission(Permissions.Employees.ChangeRole)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Reset mật khẩu của nhân viên (Dành cho Quản lý).
        /// </summary>
        /// <remarks>Yêu cầu quyền: Employees.Update.</remarks>
        /// <param name="command">Thông tin reset mật khẩu.</param>
        /// <response code="200">Reset thành công.</response>
        [HttpPost("reset-password")]
        [HasPermission(Permissions.Employees.Update)]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetEmployeePassword(
            [FromBody] ResetEmployeePasswordCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
