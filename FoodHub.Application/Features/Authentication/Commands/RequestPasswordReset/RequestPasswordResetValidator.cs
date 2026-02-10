using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetValidator(IMessageService messageService)
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.EmployeeCodeRequired))
                .Matches(@"^[WCBM]\d{6}$")
                .WithMessage(messageService.GetMessage(MessageKeys.Employee.CodeInvalidFormat));
        }
    }
}
