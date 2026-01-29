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

            // Refresh Token chỉ trả về qua Response Body, client tự quản lý
            return Ok(result.Data);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] Application.Features.Authentication.Commands.RefreshToken.RefreshTokenCommand command)
        {
            if (string.IsNullOrEmpty(command.RefreshToken))
            {
                 return BadRequest(new { message = "Refresh Token is required." });
            }

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            // Refresh Token chỉ trả về qua Response Body
            return Ok(result.Data);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Optional: Implement Revoke token logic here if needed
            // Client sẽ tự xóa token từ storage
            return NoContent();
        }


    }
}
