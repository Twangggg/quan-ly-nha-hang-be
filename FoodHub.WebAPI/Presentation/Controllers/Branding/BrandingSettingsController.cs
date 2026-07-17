using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Branding.Settings.Commands.UpdateBrandingSettings;
using FoodHub.Application.Features.Branding.Settings.Queries.GetBrandingSettings;
using FoodHub.Application.Interfaces.Common;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [Tags("He thong - Branding")]
    public class BrandingSettingsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public BrandingSettingsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/branding/settings")]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<GetBrandingSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBrandingSettings()
        {
            var result = await _mediator.Send(new GetBrandingSettingsQuery());
            return HandleResult(result);
        }

        [HttpPut("/api/v{version:apiVersion}/branding/settings")]
        [HasPermission(Permissions.Settings.Manage)]
        [ProducesResponseType(typeof(Result<UpdateBrandingSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateBrandingSettings([FromBody] UpdateBrandingSettingsCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
