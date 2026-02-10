using System.Security.Claims;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using FoodHub.WebAPI.Presentation.Attributes;

namespace FoodHub.Presentation.Controllers
{

    [Authorize]
    [Tags("Profile")]
    [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
    public class ProfileController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { message = "User not found" });

            var result = await _mediator.Send(new Query(userId));
            return HandleResult(result);
        }

        [Authorize]
        [HttpPut]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileCommand command)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { message = "User not found" });

            var result = await _mediator.Send(new UpdateProfileCommand(
                userId,
                command.FullName?.Trim(),
                command.Email?.Trim(),
                command.Phone?.Trim(),
                command.Address?.Trim(),
                command.DateOfBirth
                ));

            return HandleResult(result);
        }
    }
}
