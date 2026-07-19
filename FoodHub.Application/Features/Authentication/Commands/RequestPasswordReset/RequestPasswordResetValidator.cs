using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetValidator(IMessageService messageService)
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.EmployeeCodeRequired))
                .Matches(@"^[WCBMA]\d{6}$")
                .WithMessage(messageService.GetMessage(MessageKeys.Employee.CodeInvalidFormat));
        }
    }
}
