using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using FoodHub.Application.Features.Authentication.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly FoodHub.Application.Interfaces.ITokenService _tokenService;

        public AuthController(IMediator mediator, FoodHub.Application.Interfaces.ITokenService tokenService)
        {
            _mediator = mediator;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var command = new LoginCommand(request.EmployeeCode, request.Password, request.RememberMe);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            SetRefreshTokenCookie(result.Data.RefreshToken, result.Data.RefreshTokenExpiresIn);
            return Ok(result.Data);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            // Fallback to body if needed, but for now strict cookie
            if (string.IsNullOrEmpty(refreshToken))
            {
                 return BadRequest(new { message = "Refresh Token is required." });
            }

            var command = new Application.Features.Authentication.Commands.RefreshToken.RefreshTokenCommand { RefreshToken = refreshToken };
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            if (result.Data != null)
            {
                SetRefreshTokenCookie(result.Data.RefreshToken, result.Data.RefreshTokenExpiresIn);
            }
            return Ok(result.Data);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            // Optional: Call Command to Revoke token in DB here if needed.

            // Clear cookie with consistent options
            Response.Cookies.Delete("refreshToken", new CookieOptions 
            { 
                 HttpOnly = true, 
                 Path = "/",
                 Secure = false, 
                 SameSite = SameSiteMode.Lax
            });

            return NoContent();
        }

        private void SetRefreshTokenCookie(string refreshToken, double expiresInSeconds)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                Path = "/",
                // Use Lax/False for Dev (HTTP), None/True for Prod (HTTPS)
                SameSite = SameSiteMode.Lax, 
                Secure = false 
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
