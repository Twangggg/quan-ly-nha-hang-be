using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Result<LoginResponseDto>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
