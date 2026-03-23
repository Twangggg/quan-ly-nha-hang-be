using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Inventory.Lots.Commands.DisposeInventoryLot
{
    public class DisposeInventoryLotValidator : AbstractValidator<DisposeInventoryLotCommand>
    {
        public DisposeInventoryLotValidator(IMessageService messageService)
        {
            RuleFor(x => x.LotId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage(messageService.GetMessage(MessageKeys.InventoryLot.QuantityMin));

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.InventoryLot.ReasonRequired))
                .MaximumLength(500)
                .WithMessage(messageService.GetMessage(MessageKeys.InventoryLot.ReasonMaxLength));
        }
    }
}
