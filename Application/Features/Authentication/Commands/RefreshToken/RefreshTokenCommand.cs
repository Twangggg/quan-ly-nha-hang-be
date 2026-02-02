using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Authentication.Commands.Login;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Result<LoginResponse>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
