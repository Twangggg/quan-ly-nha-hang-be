using FluentValidation;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(v => v.CurrentPassword)
                .NotEmpty().WithMessage("Current password must not be empty.");

            RuleFor(v => v.NewPassword)
                .NotEmpty().WithMessage("New password must not be empty.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(v => v.ConfirmPassword)
                .Equal(v => v.NewPassword).WithMessage("Password confirmation does not match.");
        }
    }
}
