using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator(IMessageService messageService)
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
