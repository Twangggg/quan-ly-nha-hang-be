using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem
{
    public class DeleteMenuItemValidator : AbstractValidator<DeleteMenuItemCommand>
    {
        public DeleteMenuItemValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("Menu item id is required.");
        }
    }
}
