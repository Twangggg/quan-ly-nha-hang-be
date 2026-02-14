using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Authentication.Commands.ChangePassword;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Features.Authentication.Commands.RefreshToken;
using FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset;
using FoodHub.Application.Features.Authentication.Commands.ResetPassword;
using FoodHub.Application.Features.Authentication.Commands.RevokeToken;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý xác thực và phân quyền (Authentication).
    /// </summary>
    [Tags("Xác thực (Auth)")]
    public class AuthController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;

        public AuthController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _env = env;
        }

        /// <summary>
        /// Đăng nhập vào hệ thống.
        /// </summary>
        /// <remarks>
        /// Trả về Access Token và Refresh Token qua Cookies (HttpOnly) và Body.
        /// </remarks>
        /// <param name="command">Thông tin đăng nhập.</param>
        /// <response code="200">Đăng nhập thành công.</response>
        /// <response code="401">Thông tin đăng nhập không chính xác.</response>
        [HttpPost("login")]
        [RateLimit(maxRequests: 5, windowMinutes: 10, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                SetTokenCookies(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Làm mới Access Token bằng Refresh Token.
        /// </summary>
        /// <remarks>Dữ liệu Refresh Token được lấy từ Cookie.</remarks>
        /// <response code="200">Làm mới token thành công.</response>
        /// <response code="401">Refresh Token hết hạn hoặc không hợp lệ.</response>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token not found." });
            }

            var command = new RefreshTokenCommand { RefreshToken = refreshToken };

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
                Secure = !isDev,
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddSeconds(response.RefreshTokenExpiresIn),
                Path = "/",
            };

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddSeconds(response.ExpiresIn),
                Path = "/",
            };

            Response.Cookies.Append("accessToken", response.AccessToken, accessCookieOptions);
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống và hủy token.
        /// </summary>
        /// <param name="command">Thông tin token cần hủy (tùy chọn).</param>
        /// <response code="204">Đăng xuất thành công.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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

        /// <summary>
        /// Đổi mật khẩu cá nhân.
        /// </summary>
        /// <remarks>Yêu cầu người dùng phải đang đăng nhập.</remarks>
        /// <param name="command">Thông tin đổi mật khẩu.</param>
        /// <response code="200">Đổi mật khẩu thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Gửi yêu cầu quên mật khẩu qua email.
        /// </summary>
        /// <param name="command">Email nhận link qua mật khẩu.</param>
        /// <response code="200">Đã gửi mail thành công.</response>
        [HttpPost("request-password-reset")]
        [RateLimit(maxRequests: 3, windowMinutes: 10, blockMinutes: 10)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset(
            [FromBody] RequestPasswordResetCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Đặt lại mật khẩu mới (Reset Password).
        /// </summary>
        /// <param name="command">Token và mật khẩu mới.</param>
        /// <response code="200">Reset thành công.</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin tài khoản đang đăng nhập.
        /// </summary>
        /// <response code="200">Trả về thông tin Profile.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(
            typeof(Result<Response>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var result = await _mediator.Send(
                new Query(userId)
            );
            return HandleResult(result);
        }
    }
}
