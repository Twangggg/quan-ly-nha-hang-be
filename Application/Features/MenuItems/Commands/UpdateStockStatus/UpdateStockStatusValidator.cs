using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusValidator : AbstractValidator<UpdateStockStatusCommand>
    {
        public UpdateStockStatusValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("Menu item ID is required");
        }
    }
}
