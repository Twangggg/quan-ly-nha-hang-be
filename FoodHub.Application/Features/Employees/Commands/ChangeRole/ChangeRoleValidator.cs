using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Employees.Commands.ChangeRole
{
    public class ChangeRoleValidator : AbstractValidator<ChangeRoleCommand>
    {
        public ChangeRoleValidator(Interfaces.IMessageService messageService)
        {
            RuleFor(x => x.EmployeeCode)
               .NotEmpty()
               .Matches(@"^[WCBM]\d{6}$")
               .WithMessage(messageService.GetMessage(MessageKeys.Employee.CodeInvalidFormat));
            RuleFor(x => x.CurrentRole).IsInEnum();
            RuleFor(x => x.NewRole).IsInEnum();
        }
    }
}
