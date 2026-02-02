using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword;

public record ChangePasswordCommand : IRequest<Result<string>>
{
    public string CurrentPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
    public string ConfirmPassword { get; init; } = default!;
}
