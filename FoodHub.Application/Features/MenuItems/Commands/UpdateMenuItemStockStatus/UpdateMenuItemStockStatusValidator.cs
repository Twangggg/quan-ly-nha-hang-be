using FluentValidation;
using FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItemStockStatus
{
    public class UpdateMenuItemStockStatusValidator : AbstractValidator<UpdateMenuItemStockStatusCommand>
    {
        public UpdateMenuItemStockStatusValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("MenuItemId is required.");
        }
    }

}
