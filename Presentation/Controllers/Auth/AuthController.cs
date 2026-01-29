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
<<<<<<< HEAD
        private readonly FoodHub.Application.Interfaces.ITokenService _tokenService;

        public AuthController(IMediator mediator, FoodHub.Application.Interfaces.ITokenService tokenService)
        {
            _mediator = mediator;
            _tokenService = tokenService;
=======

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
>>>>>>> origin/feature/profile-nhudm
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
<<<<<<< HEAD
            var command = new LoginCommand(request.EmployeeCode, request.Password, request.RememberMe);
=======
            var command = new LoginCommand(request.Username, request.Password);
>>>>>>> origin/feature/profile-nhudm
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

<<<<<<< HEAD
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
=======
>>>>>>> origin/feature/profile-nhudm
            return Ok(result.Data);
        }

        [HttpPost("logout")]
<<<<<<< HEAD
        public async Task<IActionResult> Logout()
        {
            // Optional: Implement Revoke token logic here if needed
            // Client sẽ tự xóa token từ storage
            return NoContent();
        }


=======
        [Authorize]
        public IActionResult Logout()
        {
            // Với JWT, logout được xử lý từ client side bằng cách xóa token
            // Server có thể implement token blacklist nếu cần
            return NoContent();
        }
>>>>>>> origin/feature/profile-nhudm
    }
}
