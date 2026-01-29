using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
<<<<<<< HEAD
    public record LoginCommand(string EmployeeCode, string Password, bool RememberMe) : IRequest<Result<LoginResponseDto>>;
=======
    public record LoginCommand(string Username, string Password) : IRequest<Result<LoginResponseDto>>;
>>>>>>> origin/feature/profile-nhudm
}
