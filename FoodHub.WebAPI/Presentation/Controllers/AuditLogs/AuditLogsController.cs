using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.AuditLogs
{
    /// <summary>
    /// Quản lý Audit Logs (Nhật ký hệ thống).
    /// </summary>
    [Tags("Audit Logs")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class AuditLogsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách Audit Logs với phân trang và bộ lọc.
        /// </summary>
        /// <param name="query">Bộ lọc và tham số phân trang cho Audit Logs.</param>
        /// <response code="200">Trả về danh sách Audit Logs theo yêu cầu.</response>
        /// <response code="403">Không có quyền truy cập.</response>
        [HttpGet]
        [HasPermission(Permissions.Employees.ViewAuditLogs)]
        [ProducesResponseType(typeof(Result<PagedResult<GetAuditLogsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] GetAuditLogsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }
    }
}

