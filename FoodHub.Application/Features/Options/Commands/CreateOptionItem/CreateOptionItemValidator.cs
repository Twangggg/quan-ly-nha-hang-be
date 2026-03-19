using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemValidator : AbstractValidator<CreateOptionItemCommand>
    {
        public CreateOptionItemValidator(IMessageService messageService)
        {
            RuleFor(x => x.OptionGroupId).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.OptionGroup.Required));

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
