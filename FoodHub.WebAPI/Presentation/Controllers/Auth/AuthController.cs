using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Features.Authentication.Commands.RefreshToken;
using FoodHub.Application.Features.Authentication.Commands.ChangePassword;
using FoodHub.Application.Features.Authentication.Commands.RevokeToken;
using FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset;
using FoodHub.Application.Features.Authentication.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodHub.WebAPI.Presentation.Attributes;

namespace FoodHub.Presentation.Controllers
{
    [Tags("Auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;

        public AuthController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _env = env;
        }

        [HttpPost("login")]
        [RateLimit(maxRequests: 5, windowMinutes: 10, blockMinutes: 5)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                SetTokenCookies(result.Data);
            }

            return HandleResult(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token not found." });
            }

            var command = new RefreshTokenCommand
            {
                RefreshToken = refreshToken
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                SetTokenCookies(result.Data);
            }

            return HandleResult(result);
        }


        private void SetTokenCookies(LoginResponse response)
        {
            var isDev = _env.IsDevelopment();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddSeconds(response.RefreshTokenExpiresIn),
                SameSite = isDev ? SameSiteMode.Unspecified : SameSiteMode.None,
                Secure = !isDev // False in Dev (allows HTTP), True in Prod
            };

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddSeconds(response.ExpiresIn),
                SameSite = isDev ? SameSiteMode.Unspecified : SameSiteMode.None,
                Secure = !isDev
            };

            Response.Cookies.Append("accessToken", response.AccessToken, accessCookieOptions);
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand command)
        {
            var refreshToken = command.RefreshToken ?? Request.Cookies["refreshToken"];

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            if (string.IsNullOrEmpty(refreshToken))
            {
                return NoContent();
            }

            var revokeCommand = new RevokeTokenCommand { RefreshToken = refreshToken };
            await _mediator.Send(revokeCommand);

            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("request-password-reset")]
        [RateLimit(maxRequests: 3, windowMinutes: 10, blockMinutes: 10)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var result = await _mediator.Send(new FoodHub.Application.Features.Employees.Queries.GetMyProfile.Query(userId));
            return HandleResult(result);
        }
    }
}
