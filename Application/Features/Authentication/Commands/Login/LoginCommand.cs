using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string Username, string Password) : IRequest<Result<LoginResponseDto>>;
}
