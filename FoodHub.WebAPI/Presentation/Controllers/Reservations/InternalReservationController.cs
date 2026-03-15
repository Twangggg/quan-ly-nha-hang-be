using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation;
using FoodHub.Application.Features.Reservations.Commands.UpdateReservation;
using FoodHub.Application.Features.Reservations.Commands.CancelReservation;
using FoodHub.Application.Features.Reservations.Queries.GetReservations;
using FoodHub.Presentation.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
    // [Authorize(Roles = "Manager, Cashier")] // Giả sử Internal User Roles
    [Authorize]
    [Route("api/v{version:apiVersion}/reservations")]
    public class InternalReservationController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InternalReservationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách đặt bàn (Dành cho Quản lý / Thu ngân).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<ReservationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReservations([FromQuery] GetReservationsQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới đơn đặt bàn nội bộ.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateReservation([FromBody] CreateInternalReservationCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật đơn đặt bàn.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateReservation(Guid id, [FromBody] UpdateReservationCommand command)
        {
            command.ReservationId = id; 
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Hủy đơn đặt bàn.
        /// </summary>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            var result = await _mediator.Send(new CancelReservationCommand(id));
            return HandleResult(result);
        }

    }
}
