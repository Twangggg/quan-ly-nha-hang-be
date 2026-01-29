using FluentValidation;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetCommandValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Employee code is required.")
                .MaximumLength(10).WithMessage("Employee code cannot exceed 10 characters.");
        }
    }
}
