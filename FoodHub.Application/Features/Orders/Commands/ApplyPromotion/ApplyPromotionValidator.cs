using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Orders.Commands.ApplyPromotion
{
    public class ApplyPromotionValidator : AbstractValidator<ApplyPromotionCommand>
    {
        public ApplyPromotionValidator(IMessageService messageService)
        {
            RuleFor(v => v.Code)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.Required, "Code"))
                .MaximumLength(50)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidFormat));

            RuleFor(v => v.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.Required, "OrderId"));
        }
    }
}
