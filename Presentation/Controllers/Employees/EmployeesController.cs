using FoodHub.Application.Extensions.Pagination;
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
    }
}
