using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenuStockStatus
{
    public class UpdateSetMenuStockStatusValidator : AbstractValidator<UpdateSetMenuStockStatusCommand>
    {
        public UpdateSetMenuStockStatusValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("Set menu ID is required");
        }
    }
}
