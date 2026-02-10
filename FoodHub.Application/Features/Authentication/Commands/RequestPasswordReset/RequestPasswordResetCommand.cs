using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public record RequestPasswordResetCommand(string EmployeeCode) : IRequest<Result<string>>;
}
