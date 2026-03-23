using FluentValidation;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucherActive
{
    public class UpdateVoucherActiveValidator : AbstractValidator<UpdateVoucherActiveCommand>
    {
        public UpdateVoucherActiveValidator()
        {
            RuleFor(x => x.VoucherId)
                .NotEmpty().WithMessage("Voucher ID is required.");
            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("IsActive status is required.");
        }
    }
}
