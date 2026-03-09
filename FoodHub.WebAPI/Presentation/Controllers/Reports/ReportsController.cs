using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reports.Queries.GetDailyReport;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Báo cáo kinh doanh (doanh thu, đơn hàng).
    /// </summary>
    [Tags("Báo cáo (Reports)")]
    [RateLimit(maxRequests: 60, windowMinutes: 1, blockMinutes: 5)]
    public class ReportsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Báo cáo doanh thu theo ngày.
        /// </summary>
        /// <remarks>
        /// Trả về tổng doanh thu, tổng số đơn, số đơn bị huỷ và mục tiêu doanh thu ngày
        /// (tính từ moving average của N ngày trước theo múi giờ Asia/Ho_Chi_Minh).
        ///
        /// Yêu cầu quyền: Reports.View.
        /// </remarks>
        /// <param name="date">Ngày cần xem (yyyy-MM-dd). Mặc định: hôm nay theo giờ VN.</param>
        /// <param name="movingAverageDays">Số ngày dùng để tính target. Mặc định: 30.</param>
        /// <response code="200">Trả về báo cáo ngày thành công.</response>
        [HttpGet("daily")]
        [HasPermission(Permissions.Reports.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetDailyReportResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDailyReport(
            [FromQuery] DateOnly? date,
            [FromQuery] int movingAverageDays = 30)
        {
            var query = new GetDailyReportQuery
            {
                Date = date,
                MovingAverageDays = movingAverageDays,
            };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }
    }
}
