using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings;
using FoodHub.Application.Features.Reservations.Settings.Queries.GetReservationSettings;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.Reservations
{
    /// <summary>
    /// Quản lý cấu hình đặt bàn của nhà hàng.
    /// </summary>
    [Tags("Đặt bàn - Cấu hình đặt bàn")]
    public class ReservationSettingsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ReservationSettingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/reservations/settings")]
        [HasPermission(Permissions.Reservations.View)]
        [ProducesResponseType(typeof(Result<GetReservationSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReservationSettings()
        {
            var result = await _mediator.Send(new GetReservationSettingsQuery());
            return HandleResult(result);
        }

        [HttpPut("/api/v{version:apiVersion}/reservations/settings")]
        [HasPermission(Permissions.Reservations.Update)]
        [ProducesResponseType(
            typeof(Result<UpdateReservationSettingsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> UpdateReservationSettings(
            [FromBody] UpdateReservationSettingsCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
