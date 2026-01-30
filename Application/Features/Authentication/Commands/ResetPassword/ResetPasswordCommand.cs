using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.ResetPassword
{
    public record ResetPasswordCommand(
        Guid Id,
        string Token,
        string NewPassword,
        string ConfirmPassword
    ) : IRequest<Result<string>>;
}
