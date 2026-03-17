using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Reservations.Commands.CreateReservation;
using FoodHub.Application.Features.Reservations.Queries.GetAvailableTables;
using FoodHub.Application.Features.Areas.Queries.GetPublicAreas;
using FoodHub.Presentation.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
    /// <summary>
    /// Các dịch vụ đặt bàn (Reservations) công khai dành cho khách hàng.
    /// </summary>
    [Tags("Đặt bàn - Khách hàng (Public Reservations)")]
    [AllowAnonymous]
    [Route("api/v{version:apiVersion}/public/reservations")]
    public class PublicReservationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public PublicReservationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tìm kiếm các bàn còn trống dựa trên yêu cầu của khách hàng.
        /// </summary>
        /// <param name="query">Thông tin ngày giờ, số lượng khách và khu vực mong muốn.</param>
        /// <response code="200">Tìm thấy danh sách các bàn còn trống phù hợp.</response>
        /// <response code="400">Dữ liệu yêu cầu không hợp lệ.</response>
        [HttpGet("available-tables")]
        [ProducesResponseType(typeof(Result<List<GetAvailableTablesResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableTables([FromQuery] GetAvailableTablesQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Khách hàng gửi yêu cầu đặt bàn trực tuyến.
        /// </summary>
        /// <param name="command">Thông tin khách hàng, lịch đặt và bàn đã chọn.</param>
        /// <response code="200">Đặt bàn thành công, trả về thông tin đơn đặt bàn.</response>
        /// <response code="400">Dữ liệu không hợp lệ (ví dụ: đặt trước quá ít thời gian, ngày trong quá khứ).</response>
        /// <response code="409">Trùng lịch đặt hoặc bàn đã bị người khác chọn.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Result<CreateReservationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách các khu vực đang hoạt động phục vụ cho việc đặt bàn.
        /// </summary>
        /// <response code="200">Trả về danh sách các khu vực.</response>
        [HttpGet("areas")]
        [ProducesResponseType(typeof(Result<List<GetPublicAreasResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAreas()
        {
            var result = await _mediator.Send(new GetPublicAreasQuery());
            return HandleResult(result);
        }
    }
}
