using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.TokenRequired));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.NewPasswordRequired))
                .MinimumLength(8).WithMessage(messageService.GetMessage(MessageKeys.Password.MinLength))
                .Matches(@"[A-Z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireUppercase))
                .Matches(@"[a-z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireLowercase))
                .Matches(@"[0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireDigit))
                .Matches(@"[^a-zA-Z0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireSpecial));

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.ConfirmPasswordRequired))
                .Equal(x => x.NewPassword).WithMessage(messageService.GetMessage(MessageKeys.Auth.ConfirmPasswordMismatch));
        }
    }
}
