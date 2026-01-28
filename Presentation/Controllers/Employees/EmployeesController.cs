using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Employees.Commands.CreateEmployee;
using FoodHub.Application.Features.Employees.Commands.DeleteEmployee;
using FoodHub.Application.Features.Employees.Commands.UpdateEmployee;
using FoodHub.Application.Features.Employees.Queries.GetEmployeeById;
using FoodHub.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Employees
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesAsync([FromQuery] PaginationParams pagination)
        {
            var query = new GetEmployees.Query(pagination);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(Guid id)
        {
            var query = new GetEmployeeById.Query(id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployee.Command command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = result.EmployeeId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployeeAsync(Guid id, [FromBody] UpdateEmployee.Command command)
        {
            if (id != command.EmployeeId)
            {
                return BadRequest("ID mismatch");
            }
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployeeAsync(Guid id)
        {
            var result = await _mediator.Send(new DeleteEmployee.Command(id));
            return Ok(result);
        }
    }
}
