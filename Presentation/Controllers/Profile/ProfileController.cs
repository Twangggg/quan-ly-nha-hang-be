using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Profile
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            var result = await _mediator.Send(new Query(id));
            return Ok(result);
        }

        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateProfileAsync(Guid id, [FromBody] Command command)
        //{
        //    //var userIdClaim = User.FindFirst("uid");
        //    //if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        //    //{
        //    //    return NotFound();
        //    //}
        //    if (id != command.EmployeeId)
        //    {
        //        return BadRequest("ID mismatch");
        //    }

        //    var result = await _mediator.Send(new Command(
        //        id, 
        //        command.FullName, 
        //        command.Email, 
        //        command.Phone, 
        //        command.Address, 
        //        command.DateOfBirth
        //        ));

        //    return Ok(result);
        //}

        [HttpPut]
        public async Task<IActionResult> UpdateProfileAsync(Command command)
        {
            var userIdClaim = User.FindFirst("uid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new NotFoundException("User not found");
            }

            var result = await _mediator.Send(new Command(
                userId,
                command.FullName.Trim(),
                command.Email.Trim(),
                command.Phone.Trim(),
                command.Address?.Trim(),
                command.DateOfBirth
                ));

            return Ok(result);
        }
    }
}
