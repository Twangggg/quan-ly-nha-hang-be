using FluentValidation;
using FoodHub.Application.Resources;
using Microsoft.Extensions.Localization;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupValidator : AbstractValidator<CreateOptionGroupCommand>
    {
        public CreateOptionGroupValidator(IStringLocalizer<ErrorMessages> localizer)
        {
            RuleFor(x => x.MenuItemId)
                .Must(menuItemId => !menuItemId.HasValue || menuItemId.Value != Guid.Empty)
                .WithMessage(localizer["OptionGroup.MenuItemIdRequired"]);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["OptionGroup.NameRequired"])
                .MaximumLength(100).WithMessage(localizer["OptionGroup.NameRequired"]);

            RuleFor(x => x.MinSelect)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MinSelect.HasValue);

            RuleFor(x => x.MaxSelect)
                .GreaterThan(0)
                .When(x => x.MaxSelect.HasValue);

            RuleFor(x => x)
                .Must(x => !x.MinSelect.HasValue || !x.MaxSelect.HasValue || x.MinSelect <= x.MaxSelect)
                .WithMessage(localizer["OptionGroup.CannotHaveBothMinAndMax"]);
        }
    }
}
