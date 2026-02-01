using FluentValidation;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.Username).MaximumLength(50);
            RuleFor(x => x.FullName).NotEmpty();
            RuleFor(x => x.Phone).MaximumLength(15);
            RuleFor(x => x.Address).MaximumLength(255);
        }
    }
}
