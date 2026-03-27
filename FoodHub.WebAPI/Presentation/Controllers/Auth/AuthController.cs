using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.ChangePassword;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Features.Authentication.Commands.RefreshToken;
using FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset;
using FoodHub.Application.Features.Authentication.Commands.ResetPassword;
using FoodHub.Application.Features.Authentication.Commands.RevokeToken;
using FoodHub.Application.Features.Authentication.Queries.VerifyResetToken;
using FoodHub.Application.Features.Employees.Queries.GetMyProfile;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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
        private readonly IMessageService _messageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMediator mediator,
            IWebHostEnvironment env,
            IMessageService messageService,
            IConfiguration configuration,
            ILogger<AuthController> logger
        ) : base(messageService)
        {
            _mediator = mediator;
            _env = env;
            _messageService = messageService;
            _configuration = configuration;
            _logger = logger;
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
        [RateLimit(maxRequests: 50, windowMinutes: 10, blockMinutes: 5)]
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
                return Unauthorized(
                    new ErrorResponse(
                        StatusCodes.Status401Unauthorized,
                        _messageService.GetMessage(MessageKeys.Auth.RefreshTokenNotFound)
                    )
                );
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
            var enableHttps = _configuration.GetValue<bool>("EnableHttpsRedirection", true);
            var isSecure = !isDev && enableHttps;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddSeconds(response.RefreshTokenExpiresIn),
                Path = "/",
            };

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
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
        public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RevokeTokenCommand? command)
        {
            var refreshToken = command?.RefreshToken ?? Request.Cookies["refreshToken"];

            var isDev = _env.IsDevelopment();
            var enableHttps = _configuration.GetValue<bool>("EnableHttpsRedirection", true);
            var isSecure = !isDev && enableHttps;

            var deleteOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            };

            Response.Cookies.Delete("accessToken", deleteOptions);
            Response.Cookies.Delete("refreshToken", deleteOptions);

            if (string.IsNullOrEmpty(refreshToken))
            {
                return NoContent();
            }

            try 
            {
                var revokeCommand = new RevokeTokenCommand { RefreshToken = refreshToken };
                await _mediator.Send(revokeCommand);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token during logout");
            }

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
        [RateLimit(maxRequests: 20, windowMinutes: 10, blockMinutes: 5)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestPasswordReset(
            [FromBody] RequestPasswordResetCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Xác minh token đặt lại mật khẩu có còn hợp lệ không.
        /// </summary>
        /// <remarks>Gọi trước khi hiển thị form đổi mật khẩu để kiểm tra token chưa hết hạn và chưa sử dụng.</remarks>
        /// <param name="token">Token từ link email.</param>
        /// <response code="200">Trả về true nếu token hợp lệ, false nếu không.</response>
        [HttpGet("verify-reset-token")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyResetToken([FromQuery] string token)
        {
            var result = await _mediator.Send(new VerifyResetTokenQuery(token));
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
        [ProducesResponseType(typeof(Result<Response>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(
                    new ErrorResponse(
                        StatusCodes.Status401Unauthorized,
                        _messageService.GetMessage(MessageKeys.Auth.InvalidTokenClaims)
                    )
                );
            }

            var result = await _mediator.Send(new Query(userId));
            return HandleResult(result);
        }
    }
}
