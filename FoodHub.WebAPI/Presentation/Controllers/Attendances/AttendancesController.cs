using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport;
using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Controller quản lý các hoạt động báo cáo chấm công.
    /// </summary>
    [Tags("Chấm công (Attendances)")]
    [RateLimit(maxRequests: 60, windowMinutes: 1, blockMinutes: 5)]
    public class AttendancesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public AttendancesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy báo cáo chấm công nhân viên (Phân trang và lọc).
        /// </summary>
        /// <param name="pagination">Tham số phân trang và lọc (Search, Filters, OrderBy).</param>
        /// <returns>Danh sách báo cáo chấm công đã được phân trang.</returns>
        [HttpGet("report")]
        [HasPermission(Permissions.Attendances.View)]
        [ProducesResponseType(typeof(Result<PagedResult<GetAttendanceReportResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAttendanceReport([FromQuery] PaginationParams pagination)
        {
            var query = new GetAttendanceReportQuery(pagination);
            var result = await _mediator.Send(query);
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Xuất báo cáo chấm công ra file Excel.
        /// </summary>
        /// <param name="pagination">Tham số lọc (giống với endpoint list).</param>
        /// <returns>File Excel (.xlsx).</returns>
        [HttpGet("report/export")]
        [HasPermission(Permissions.Attendances.View)]
        [Produces(System.Net.Mime.MediaTypeNames.Application.Octet)]
        public async Task<IActionResult> ExportAttendanceReport([FromQuery] PaginationParams pagination)
        {
            var query = new ExportAttendanceReportQuery(pagination);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return HandleResult(result);

            return File(
                result.Data!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
