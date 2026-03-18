using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck
{
    public class ProcessInventoryCheckValidator : AbstractValidator<ProcessInventoryCheckCommand>
    {
        public ProcessInventoryCheckValidator(IMessageService messageService)
        {
            RuleFor(x => x.InventoryCheckId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
