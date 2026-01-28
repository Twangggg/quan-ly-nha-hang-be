using FluentValidation;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Mã nhân viên không được để trống");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(3).WithMessage("Mật khẩu quá ngắn");
        }
    }
}
