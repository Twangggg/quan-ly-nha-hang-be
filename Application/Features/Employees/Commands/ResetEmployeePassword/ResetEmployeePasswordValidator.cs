using FluentValidation;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public class ResetEmployeePasswordValidator : AbstractValidator<ResetEmployeePasswordCommand>
    {
        public ResetEmployeePasswordValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("EmployeeId không được để trống");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Lý do reset password là bắt buộc")
                .MinimumLength(10).WithMessage("Lý do reset phải có ít nhất 10 ký tự")
                .MaximumLength(500).WithMessage("Lý do reset không được quá 500 ký tự");
            When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
            {
                RuleFor(x => x.NewPassword)
                    .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
                    .Matches(@"[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ hoa")
                    .Matches(@"[a-z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ thường")
                    .Matches(@"[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 số")
                    .Matches(@"[\W_]").WithMessage("Mật khẩu phải có ít nhất 1 ký tự đặc biệt");
            });
        }
    }
}
