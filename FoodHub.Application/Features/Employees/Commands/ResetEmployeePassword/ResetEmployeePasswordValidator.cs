using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public class ResetEmployeePasswordValidator : AbstractValidator<ResetEmployeePasswordCommand>
    {
        public ResetEmployeePasswordValidator(IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Password.EmployeeIdRequired));

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Password.ReasonRequired))
                .MinimumLength(10).WithMessage(messageService.GetMessage(MessageKeys.Password.ReasonMinLength))
                .MaximumLength(500).WithMessage(messageService.GetMessage(MessageKeys.Password.ReasonMaxLength));

            When(
                x => !string.IsNullOrEmpty(x.NewPassword),
                () =>
                {
                    RuleFor(x => x.NewPassword)
                        .MinimumLength(8).WithMessage(messageService.GetMessage(MessageKeys.Password.MinLength))
                        .Matches(@"[A-Z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireUppercase))
                        .Matches(@"[a-z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireLowercase))
                        .Matches(@"[0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireDigit))
                        .Matches(@"[\W_]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireSpecial));
                }
            );
        }
    }
}
