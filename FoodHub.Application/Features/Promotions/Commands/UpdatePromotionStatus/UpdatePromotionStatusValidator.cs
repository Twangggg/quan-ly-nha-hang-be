using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Promotions.Commands.UpdatePromotionStatus
{
    public class UpdatePromotionStatusValidator : AbstractValidator<UpdatePromotionStatusCommand>
    {
        public UpdatePromotionStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.PromotionId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "PromotionId" }));
        }
    }
}
