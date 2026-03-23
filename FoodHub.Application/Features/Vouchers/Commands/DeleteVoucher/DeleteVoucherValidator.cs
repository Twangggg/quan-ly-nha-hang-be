using FluentValidation;

namespace FoodHub.Application.Features.Vouchers.Commands.DeleteVoucher
{
    public class DeleteVoucherValidator : AbstractValidator<DeleteVoucherCommand>
    {
        public DeleteVoucherValidator()
        {
            RuleFor(x => x.VoucherId)
                .NotEmpty().WithMessage("Voucher ID is required.");
        }
    }
}
