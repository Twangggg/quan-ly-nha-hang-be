using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemValidator : AbstractValidator<CreateOptionItemCommand>
    {
        public CreateOptionItemValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage(localizer["OptionGroup.MenuItemIdRequired"]);
            RuleFor(x => x.OptionGroupId)
                .NotEmpty().WithMessage(localizer["OptionGroup.Required"]);

            RuleFor(x => x.Label)
                .NotEmpty().WithMessage(localizer["OptionItem.LabelRequired"]);

            RuleFor(x => x.ExtraPrice)
                .GreaterThanOrEqualTo(0).WithMessage(localizer["OptionItem.ExtraPriceInvalid"]);
        }
    }
}
