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
                .NotEmpty().WithMessage(localizer["OptionGroup.MenuItemIdRequired"]);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(localizer["OptionGroup.NameRequired"]);
        }
    }
}
