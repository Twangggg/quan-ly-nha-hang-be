using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string EmployeeCode, string Password) : IRequest<Result<LoginResponseDto>>;
}
