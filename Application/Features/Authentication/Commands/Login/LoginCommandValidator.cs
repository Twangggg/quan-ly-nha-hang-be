using FluentValidation;
using FoodHub.Domain.Constants;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage(Messages.UsernameRequired)
                .MinimumLength(3).WithMessage(Messages.UsernameMinLength);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(Messages.PasswordRequired)
                .MinimumLength(6).WithMessage(Messages.PasswordMinLength);
        }
    }
}
