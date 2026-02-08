using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator(Interfaces.IMessageService messageService)
        {
            // Current password required
            RuleFor(v => v.CurrentPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Password.IncorrectCurrent));

            // New password validation
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Password.NotEmpty))
                .MinimumLength(8).WithMessage(messageService.GetMessage(MessageKeys.Password.MinLength))
                .Matches(@"[A-Z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireUppercase))
                .Matches(@"[a-z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireLowercase))
                .Matches(@"[0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireDigit))
                .Matches(@"[\W_]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireSpecial))
                .NotEqual(x => x.CurrentPassword).WithMessage(messageService.GetMessage(MessageKeys.Password.MustBeDifferent));

            // Confirmation match
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage(messageService.GetMessage(MessageKeys.Password.ConfirmationMismatch));
        }
    }
}
