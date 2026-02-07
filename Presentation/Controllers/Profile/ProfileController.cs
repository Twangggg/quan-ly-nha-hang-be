using System.Security.Claims;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Profile
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new NotFoundException("User not found");
            }

            var result = await _mediator.Send(new Query(userId));
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfileAsync(Command command)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new NotFoundException("User not found");
            }

            var result = await _mediator.Send(new Command(
                userId,
                command.FullName.Trim(),
                command.Email.Trim(),
                command.Phone?.Trim(),
                command.Address?.Trim(),
                command.DateOfBirth
                ));

            return Ok(result);
        }
    }
}
