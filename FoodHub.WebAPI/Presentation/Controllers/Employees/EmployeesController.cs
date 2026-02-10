using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Employees.Commands.CreateEmployee;
using FoodHub.Application.Features.Employees.Commands.DeleteEmployee;
using FoodHub.Application.Features.Employees.Commands.UpdateEmployee;
using FoodHub.Application.Features.Employees.Queries.GetEmployeeById;
using FoodHub.Application.Features.Employees.Queries.GetEmployees;
using FoodHub.Application.Features.Employees.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodHub.Application.Features.Employees.Commands.ChangeRole;
using FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword;

using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
    [Tags("Employees")]
    [RateLimit(maxRequests: 100, windowMinutes: 1, blockMinutes: 5)]
    public class EmployeesController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesAsync([FromQuery] PaginationParams pagination)
        {
            var query = new GetEmployeesQuery(pagination);
            var result = await _mediator.Send(query);
            
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            
            return HandleResult<PagedResult<GetEmployeesResponse>>(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(Guid id)
        {
            var query = new GetEmployeeByIdQuery(id);
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployeeCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
                return HandleResult(result);

            return CreatedAtAction(nameof(GetEmployeeById), new { id = result.Data!.EmployeeId }, result.Data);
        }

        [HttpPut("{id}")]
        [RateLimit(maxRequests: 30, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> UpdateEmployeeAsync(Guid id, [FromBody] UpdateEmployeeCommand command)
        {
            if (id != command.EmployeeId)
            {
                return BadRequest(new { message = "ID mismatch" });
            }
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        [RateLimit(maxRequests: 10, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> DeleteEmployeeAsync(Guid id)
        {
            var result = await _mediator.Send(new DeleteEmployeeCommand(id));
            return HandleResult(result);
        }

        [HttpGet("{id}/audit-logs")]
        [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
        public async Task<IActionResult> GetAuditLogs(Guid id, [FromQuery] PaginationParams pagination)
        {
            var query = new GetAuditLogsQuery(id, pagination);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult<PagedResult<GetAuditLogsResponse>>(result);
        }

        [HttpPost("change-role")]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> ResetEmployeePassword([FromBody] ResetEmployeePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
