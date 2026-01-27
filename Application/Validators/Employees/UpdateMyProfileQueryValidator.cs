using FluentValidation;
using FoodHub.Application.Features.Employees.Query;

namespace FoodHub.Application.Validators.Employees
{
    public class UpdateMyProfileQueryValidator : AbstractValidator<UpdateMyProfileQuery>
    {
        public UpdateMyProfileQueryValidator()
        {
            RuleFor(x => x.UpdateProfileDto.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự");

            RuleFor(x => x.UpdateProfileDto.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.UpdateProfileDto.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^\d{10}$").WithMessage("Số điện thoại phải có 10 chữ số");
        }
    }
}
