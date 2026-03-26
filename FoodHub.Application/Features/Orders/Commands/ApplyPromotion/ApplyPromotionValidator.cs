using FluentValidation;

namespace FoodHub.Application.Features.Orders.Commands.ApplyPromotion
{
    public class ApplyPromotionValidator : AbstractValidator<ApplyPromotionCommand>
    {
        public ApplyPromotionValidator()
        {
            RuleFor(v => v.Code)
                .NotEmpty()
                .WithMessage("Mã khuyến mãi không được để trống")
                .MaximumLength(50)
                .WithMessage("Mã khuyến mãi không vượt quá 50 ký tự");

            RuleFor(v => v.OrderId).NotEmpty().WithMessage("OrderId không được để trống");
        }
    }
}
