using FluentValidation;
using FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem;

namespace FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu
{
    public class DeleteSetMenuValidator : AbstractValidator<DeleteSetMenuCommand>
    {
        public DeleteSetMenuValidator()
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage("SetMenuId is required.");
        }
    }
}
