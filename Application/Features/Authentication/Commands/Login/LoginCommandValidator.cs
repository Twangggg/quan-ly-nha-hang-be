using FluentValidation;
<<<<<<< HEAD
=======
using FoodHub.Domain.Constants;
>>>>>>> origin/feature/profile-nhudm

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
<<<<<<< HEAD
            RuleFor(x => x.EmployeeCode)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(3);
=======
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage(Messages.UsernameRequired)
                .MinimumLength(3).WithMessage(Messages.UsernameMinLength);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(Messages.PasswordRequired)
                .MinimumLength(6).WithMessage(Messages.PasswordMinLength);
>>>>>>> origin/feature/profile-nhudm
        }
    }
}
