using System.Security.Claims;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý thông tin hồ sơ cá nhân của người dùng đang đăng nhập.
    /// </summary>
    [Authorize]
    [Tags("Hồ sơ cá nhân (Profile)")]
    [RateLimit(maxRequests: 50, windowMinutes: 1, blockMinutes: 5)]
    public class ProfileController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy thông tin hồ sơ cá nhân của tôi.
        /// </summary>
        /// <response code="200">Trả về thông tin hồ sơ.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(Result<MyProfileResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { message = "User not found" });

            var result = await _mediator.Send(new Query(userId));
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ cá nhân.
        /// </summary>
        /// <param name="command">Thông tin cập nhật mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        [Authorize]
        [HttpPut]
        [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateProfileAsync(UpdateProfileCommand command)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { message = "User not found" });

            var result = await _mediator.Send(
                new UpdateProfileCommand(
                    userId,
                    command.FullName?.Trim(),
                    command.Email?.Trim(),
                    command.Phone?.Trim(),
                    command.Address?.Trim(),
                    command.DateOfBirth
                )
            );

            return HandleResult(result);
        }
    }
}
