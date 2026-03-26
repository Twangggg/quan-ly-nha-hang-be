using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Promotions.Commands.DeletePromotion
{
    public class DeletePromotionValidator : AbstractValidator<DeletePromotionCommand>
    {
        public DeletePromotionValidator(IMessageService messageService)
        {
            RuleFor(x => x.PromotionId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "PromotionId" }));
        }
    }
}
