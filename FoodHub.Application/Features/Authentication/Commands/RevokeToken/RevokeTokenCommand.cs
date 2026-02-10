using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenCommand : IRequest<Result<bool>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
