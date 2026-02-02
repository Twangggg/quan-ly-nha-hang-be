using FluentValidation;

namespace FoodHub.Application.Features.Employees.Commands.ChangeRole
{
    public class ChangeRoleValidator : AbstractValidator<ChangeRoleCommand>
    {
        public ChangeRoleValidator()
        {
            RuleFor(x => x.EmployeeCode)
               .NotEmpty()
               .Matches(@"^[WCBM]\d{6}$")
               .WithMessage("Employee Code must start with W, C, B, or M followed by 6 digits (e.g., M000001).");
            RuleFor(x => x.CurrentRole).IsInEnum();
            RuleFor(x => x.NewRole).IsInEnum();
        }
    }
}
