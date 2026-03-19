using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemValidator : AbstractValidator<UpdateOptionItemCommand>
    {
        public UpdateOptionItemValidator(IMessageService messageService)
        {
            RuleFor(x => x.OptionItemId).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));

            RuleFor(x => x.Label)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OptionItem.LabelRequired))
                .MaximumLength(100)
                .WithMessage(messageService.GetMessage(MessageKeys.OptionItem.LabelRequired));

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage(messageService.GetMessage(MessageKeys.OptionItem.ExtraPriceInvalid));
        }
    }
}
