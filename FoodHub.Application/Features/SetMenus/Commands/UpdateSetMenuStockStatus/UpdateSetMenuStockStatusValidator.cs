using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItemStockStatus
{
    public class UpdateSetMenuStockStatusValidator : AbstractValidator<UpdateSetMenuStockStatusCommand>
    {
        public UpdateSetMenuStockStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.IdRequired));
        }
    }
}
