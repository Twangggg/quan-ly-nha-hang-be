using FluentValidation;

namespace FoodHub.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(v => v.CurrentPassword)
                .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

            RuleFor(v => v.NewPassword)
                .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
                .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
                .Matches("[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
                .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một số.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");

            RuleFor(v => v.ConfirmPassword)
                .Equal(v => v.NewPassword).WithMessage("Mật khẩu nhập lại không khớp.");
        }
    }
}
