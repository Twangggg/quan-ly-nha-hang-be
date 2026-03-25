using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Orders.Commands.UnapplyPromotion
{
    public class UnapplyPromotionValidator : AbstractValidator<UnapplyPromotionCommand>
    {
        public UnapplyPromotionValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "OrderId" }));
        }
    }
}
