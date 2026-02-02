using FluentValidation;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetCommandValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Employee code is required.")
                .Matches(@"^[WCBM]\d{6}$")
                .WithMessage("Invalid Employee code format.");
        }
    }
}
