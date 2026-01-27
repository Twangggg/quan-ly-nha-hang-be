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

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var command = new LoginCommand(request.Username, request.Password);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Với JWT, logout được xử lý từ client side bằng cách xóa token
            // Server có thể implement token blacklist nếu cần
            return NoContent();
        }
    }
}
