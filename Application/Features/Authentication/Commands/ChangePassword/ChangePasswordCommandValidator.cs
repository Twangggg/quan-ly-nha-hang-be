using FluentValidation;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            // Only validate that current password is provided
            // All other validations are in the handler to enable rate limiting
            RuleFor(v => v.CurrentPassword)
                .NotEmpty().WithMessage("Current password is incorrect.");
        }
    }
}
