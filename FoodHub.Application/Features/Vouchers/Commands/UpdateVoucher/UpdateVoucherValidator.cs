using FluentValidation;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucher
{
    public class UpdateVoucherValidator : AbstractValidator<UpdateVoucherCommand>
    {
        public UpdateVoucherValidator()
        {
            RuleFor(x => x.VoucherId)
                .NotEmpty().WithMessage("Voucher ID is required.");
            RuleFor(x => x.VoucherCode)
                .NotEmpty().WithMessage("Voucher code is required.")
                .MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.");
            RuleFor(x => x.VoucherType)
                .IsInEnum().WithMessage("Invalid voucher type.");
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).When(x => x.VoucherType == VoucherType.Percent || x.VoucherType == VoucherType.Fixed)
                .WithMessage("Discount value must be greater than 0 for percentage or fixed amount vouchers.");
            RuleFor(x => x.MaxDiscount)
                .GreaterThan(0).When(x => x.VoucherType == VoucherType.Percent)
                .WithMessage("Max discount must be greater than 0 for percentage vouchers.");
            RuleFor(x => x.MinOrderValue)
                .GreaterThanOrEqualTo(0).WithMessage("Min order value must be greater than or equal to 0.");
            RuleFor(x => x.ItemtId)
                .NotNull().When(x => x.VoucherType == VoucherType.FreeItem)
                .WithMessage("Item ID is required for free item vouchers.");
            RuleFor(x => x.FreeQuantity)
                .GreaterThan(0).When(x => x.VoucherType == VoucherType.FreeItem)
                .WithMessage("Free quantity must be greater than 0 for free item vouchers.");
            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
            RuleFor(x => x.StartTime)
                .LessThan(x => x.EndTime).When(x => x.StartTime.HasValue && x.EndTime.HasValue)
                .WithMessage("Start time must be before end time when both are specified.");
            RuleFor(x => x.EndTime)
                .GreaterThanOrEqualTo(TimeSpan.Zero).When(x => x.EndTime.HasValue)
                .WithMessage("End time must be greater than or equal to 00:00:00.");
            RuleFor(x => x.UsageLimit)
                .GreaterThanOrEqualTo(0).WithMessage("Usage limit must be greater than or equal to 0.");
            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("IsActive is required.");
        }
    }
}
