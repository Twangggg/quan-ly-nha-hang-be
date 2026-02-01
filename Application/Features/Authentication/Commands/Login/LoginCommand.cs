using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string EmployeeCode, string Password, bool RememberMe) : IRequest<Result<LoginResponse>>;
}
