using MediatR;
using FoodHub.Application.Common.Models;

namespace FoodHub.Application.Features.Auth.Commands.ChangePassword
{
    public record ChangePasswordCommand : IRequest<Result<string>>
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
