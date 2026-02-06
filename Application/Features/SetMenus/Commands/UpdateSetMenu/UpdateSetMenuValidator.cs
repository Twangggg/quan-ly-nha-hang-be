using FluentValidation;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class DeleteSetMenuValidator : AbstractValidator<DeleteSetMenuCommand>
    {
        public DeleteSetMenuValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Items)
                .NotNull().WithMessage("Items list cannot be null.")
                .Must(items => items != null && items.Any()).WithMessage("At least one menu item is required.");

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            });

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.MenuItemId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate menu items are not allowed.");

        }
    }
}
