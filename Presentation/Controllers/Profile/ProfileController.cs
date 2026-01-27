using FoodHub.Application.DTOs.Employees;
using FoodHub.Application.Features.Employees.Query;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Profile
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            //var userIdClaim = User.FindFirst("uid");x
            //if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            //{
            //    return NotFound();
            //}
            Guid userId = Guid.Parse("c8f1a2b4-6d3e-4f9a-b7c2-5e1d8a9f0b6c");

            var result = await _mediator.Send(new UpdateMyProfileQuery(userId, dto));

            return Ok(result);
        }
    }
}
