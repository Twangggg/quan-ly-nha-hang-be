using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuValidator : AbstractValidator<UpdateSetMenuCommand>
    {
        public UpdateSetMenuValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("Set menu ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Set menu name is required")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Set menu must contain at least one item")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("Set menu must contain at least one item");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.MenuItemId)
                    .NotEmpty().WithMessage("Menu item ID is required");

                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0");
            });
        }
    }
}
