using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Reservations.Commands.CreateReservation;
using FoodHub.Application.Features.Reservations.Queries.GetAvailableTables;
using FoodHub.Presentation.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
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
        /// Lấy danh sách bàn còn trống để khách hàng chọn.
        /// </summary>
        /// <param name="query">Thông tin ngày giờ và khu vực khách muốn đặt, số lượng khách</param>
        /// <response code="200">Tìm thấy các bàn trống.</response>
        [HttpGet("available-tables")]
        [ProducesResponseType(typeof(Result<List<GetAvailableTablesResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableTables([FromQuery] GetAvailableTablesQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Gửi yêu cầu đặt bàn trực tuyến từ trang chủ.
        /// </summary>
        /// <param name="command">Thông tin khách hàng và lịch đặt</param>
        /// <response code="200">Đặt bàn thành công, trả về ID.</response>
        /// <response code="400">Dữ liệu không hợp lệ (Lỗi 45 phút, ngày quá khứ, v.v.).</response>
        /// <response code="409">Lỗi trùng lịch đặt.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
