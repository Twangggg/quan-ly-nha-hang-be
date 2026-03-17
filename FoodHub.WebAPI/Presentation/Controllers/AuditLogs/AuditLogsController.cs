using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.WebAPI.Presentation.Controllers.AuditLogs
{
    [Authorize(Roles = "Manager,Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<Result<PagedResult<GetAuditLogsResponse>>>> GetAuditLogs(
            [FromQuery] GetAuditLogsQuery query)
        {
            return Ok(await _mediator.Send(query));
        }
    }
}
