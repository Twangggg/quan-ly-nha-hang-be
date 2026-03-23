using FluentValidation;

namespace FoodHub.Application.Features.Options.Commands.UpdateMenuItemOptionGroup
{
    public class UpdateMenuItemOptionGroupValidator
        : AbstractValidator<UpdateMenuItemOptionGroupCommand>
    {
        public UpdateMenuItemOptionGroupValidator()
        {
            RuleFor(x => x.MenuItemOptionGroupId).NotEmpty();
            RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxSelect).GreaterThan(0);
            RuleFor(x => x.MaxSelect).GreaterThanOrEqualTo(x => x.MinSelect);
        }
    }
}
