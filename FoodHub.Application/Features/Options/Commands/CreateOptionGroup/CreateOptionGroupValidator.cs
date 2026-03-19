using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupValidator : AbstractValidator<CreateOptionGroupCommand>
    {
        public CreateOptionGroupValidator(IMessageService messageService)
        {
            RuleFor(x => x.MenuItemId)
                .Must(menuItemId => !menuItemId.HasValue || menuItemId.Value != Guid.Empty)
                .WithMessage(messageService.GetMessage("OptionGroup.MenuItemIdRequired"));

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OptionGroup.NameRequired))
                .MaximumLength(100)
                .WithMessage(messageService.GetMessage(MessageKeys.OptionGroup.NameRequired));

            RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0).When(x => x.MinSelect.HasValue);

            RuleFor(x => x.MaxSelect).GreaterThan(0).When(x => x.MaxSelect.HasValue);

            RuleFor(x => x)
                .Must(x =>
                    !x.MinSelect.HasValue || !x.MaxSelect.HasValue || x.MinSelect <= x.MaxSelect
                )
                .WithMessage(messageService.GetMessage("OptionGroup.CannotHaveBothMinAndMax"));
        }
    }
}
