using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.AssignOptionGroupToMenuItem
{
    public class AssignOptionGroupToMenuItemValidator
        : AbstractValidator<AssignOptionGroupToMenuItemCommand>
    {
        public AssignOptionGroupToMenuItemValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty()
                .WithMessage(localizer["OptionGroup.MenuItemIdRequired"]);

            RuleFor(x => x.OptionGroupId)
                .NotEmpty()
                .WithMessage(localizer["OptionGroup.Required"]);

            RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxSelect).GreaterThan(0);
            RuleFor(x => x.MaxSelect).GreaterThanOrEqualTo(x => x.MinSelect);
        }
    }
}
