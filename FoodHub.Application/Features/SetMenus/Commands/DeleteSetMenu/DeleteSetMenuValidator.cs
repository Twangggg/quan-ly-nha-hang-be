using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.SetMenus.Commands.DeleteSetMenu
{
    public class DeleteSetMenuValidator : AbstractValidator<DeleteSetMenuCommand>
    {
        public DeleteSetMenuValidator(IMessageService messageService)
        {
            RuleFor(x => x.SetMenuId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.SetMenu.IdRequired));
        }
    }
}
