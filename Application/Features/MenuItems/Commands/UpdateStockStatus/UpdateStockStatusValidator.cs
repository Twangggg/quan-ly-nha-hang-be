using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public class UpdateStockStatusValidator : AbstractValidator<UpdateStockStatusCommand>
    {
        public UpdateStockStatusValidator()
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty().WithMessage("ID món ăn không được để trống");
        }
    }
}
