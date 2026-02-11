using FluentValidation;
using FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItemStockStatus
{
    public class UpdateSetMenuStockStatusValidator : AbstractValidator<UpdateSetMenuStockStatusCommand>
    {
        public UpdateSetMenuStockStatusValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");
        }
    }

}
