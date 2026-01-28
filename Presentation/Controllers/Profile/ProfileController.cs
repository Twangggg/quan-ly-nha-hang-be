using FoodHub.Application.Features.Employees.Commands;
using FoodHub.Application.Features.Employees.Query;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Profile
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ApiControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetProfile(
            [FromHeader(Name = "x-user-id")] Guid userId)
        {
            var result = await Mediator.Send(new GetMyProfileQuery(userId));
            return Ok(result);
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody]UpdateProfileRequest dto,
            [FromHeader(Name = "x-user-id")] Guid userId)
        {
            //var userIdClaim = User.FindFirst("uid");x
            //if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            //{
            //    return NotFound();
            //}

            var result = await Mediator.Send(new UpdateMyProfileCommand(userId, dto));

            return Ok(result);
        }
    }
}
