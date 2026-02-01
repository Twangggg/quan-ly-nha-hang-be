using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.ResetPassword
{
    public record ResetPasswordCommand(
        string Token,
        string NewPassword,
        string ConfirmPassword
    ) : IRequest<Result<string>>;
}
