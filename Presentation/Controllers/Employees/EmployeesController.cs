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

namespace FoodHub.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
    [Tags("Employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                if (result.HasWarning)
                {
                    return Ok(new
                    {
                        data = result.Data,
                        warning = result.Warning
                    });
                }
                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { message = result.Error }),
                ResultErrorType.Unauthorized => Unauthorized(new { message = result.Error }),
                ResultErrorType.Forbidden => Forbid(),
                ResultErrorType.Conflict => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesAsync([FromQuery] PaginationParams pagination)
        {
            var query = new GetEmployeesQuery(pagination);
            var result = await _mediator.Send(query);
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
        public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployeeCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
                return HandleResult(result);

            return CreatedAtAction(nameof(GetEmployeeById), new { id = result.Data!.EmployeeId }, result.Data);
        }

        [HttpPut("{id}")]
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
        public async Task<IActionResult> DeleteEmployeeAsync(Guid id)
        {
            var result = await _mediator.Send(new DeleteEmployeeCommand(id));
            return HandleResult(result);
        }

        [HttpGet("{id}/audit-logs")]
        public async Task<IActionResult> GetAuditLogs(Guid id, [FromQuery] PaginationParams pagination)
        {
            var query = new GetAuditLogsQuery(id, pagination);
            var result = await _mediator.Send(query);
            return HandleResult<PagedResult<GetAuditLogsResponse>>(result);
        }

        [HttpPost("change-role")]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetEmployeePassword([FromBody] ResetEmployeePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
