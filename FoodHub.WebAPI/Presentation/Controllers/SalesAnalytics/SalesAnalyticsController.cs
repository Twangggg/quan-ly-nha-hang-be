using System.Net.Mime;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport;
using FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Báo cáo kinh doanh (doanh thu, đơn hàng).
    /// </summary>
    [Tags("Phân tích & Thống kê (SalesAnalytics)")]
    [RateLimit(maxRequests: 60, windowMinutes: 1, blockMinutes: 5)]
    public class SalesAnalyticsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SalesAnalyticsController(IMediator mediator)
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
        [HttpGet("summary")]
        [HasPermission(Permissions.SalesAnalytics.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetDailyReportResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDailyReport(
            [FromQuery] DateOnly? date,
            [FromQuery] int movingAverageDays = 30
        )
        {
            var query = new GetDailyReportQuery
            {
                Date = date,
                MovingAverageDays = movingAverageDays,
            };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Báo cáo doanh thu theo tháng.
        /// </summary>
        /// <remarks>
        /// Trả về tổng doanh thu, tổng số đơn và số đơn bị huỷ trong một tháng.
        ///
        /// Yêu cầu quyền: Reports.View.
        /// </remarks>
        /// <param name="year">Năm cần xem. Mặc định: Năm hiện tại theo giờ VN.</param>
        /// <param name="month">Tháng cần xem (1-12). Mặc định: Tháng hiện tại theo giờ VN.</param>
        /// <response code="200">Trả về báo cáo tháng thành công.</response>
        [HttpGet("monthly-summary")]
        [HasPermission(Permissions.SalesAnalytics.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetMonthlyReportResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlyReport(
            [FromQuery] int? year,
            [FromQuery] int? month
        )
        {
            var query = new GetMonthlyReportQuery { Year = year, Month = month };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Báo cáo món bán chạy nhất.
        /// </summary>
        /// <remarks>
        /// Trả về danh sách món ăn kèm số lượng, doanh thu và lợi nhuận gộp.
        ///
        /// Yêu cầu quyền: Reports.View.
        /// </remarks>
        /// <param name="startDate">Ngày bắt đầu (yyyy-MM-dd).</param>
        /// <param name="endDate">Ngày kết thúc (yyyy-MM-dd).</param>
        /// <param name="top">Số lượng món muốn lấy. Mặc định: 10.</param>
        /// <response code="200">Trả về danh sách món bán chạy thành công.</response>
        [HttpGet("best-sellers")]
        [HasPermission(Permissions.SalesAnalytics.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetBestSellersResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBestSellers(
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] int top = 10
        )
        {
            var query = new GetBestSellersQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                Top = top,
            };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Báo cáo doanh thu theo danh mục.
        /// </summary>
        /// <param name="startDate">Ngày bắt đầu (yyyy-MM-dd).</param>
        /// <param name="endDate">Ngày kết thúc (yyyy-MM-dd).</param>
        /// <response code="200">Trả về báo cáo doanh thu theo danh mục thành công.</response>
        [HttpGet("category-report")]
        [HasPermission(Permissions.SalesAnalytics.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetCategoryReportResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoryReport(
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate
        )
        {
            var query = new GetCategoryReportQuery { StartDate = startDate, EndDate = endDate };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Báo cáo biểu đồ doanh thu.
        /// </summary>
        /// <param name="date">Ngày (yyyy-MM-dd) cho báo cáo theo giờ.</param>
        /// <param name="year">Năm cho báo cáo theo tháng.</param>
        /// <param name="month">Tháng cho báo cáo theo tháng.</param>
        /// <response code="200">Trả về dữ liệu biểu đồ doanh thu thành công.</response>
        [HttpGet("revenue-chart")]
        [HasPermission(Permissions.SalesAnalytics.View)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Result<GetRevenueChartResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueChart(
            [FromQuery] DateOnly? date,
            [FromQuery] int? year,
            [FromQuery] int? month
        )
        {
            var query = new GetRevenueChartQuery
            {
                Date = date,
                Year = year,
                Month = month,
            };
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }
    }
}
