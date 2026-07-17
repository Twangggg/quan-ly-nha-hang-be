using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Settings.Commands.UpdateKdsSettings;
using FoodHub.Application.Features.KDS.Settings.Queries.GetKdsSettings;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.KDS
{
    [Tags("Kitchen Display System (KDS) - Settings")]
    public class KdsSettingsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public KdsSettingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/kds/settings")]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<GetKdsSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKdsSettings()
        {
            var result = await _mediator.Send(new GetKdsSettingsQuery());
            return HandleResult(result);
        }

        [HttpPut("/api/v{version:apiVersion}/kds/settings")]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<UpdateKdsSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateKdsSettings([FromBody] UpdateKdsSettingsCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
