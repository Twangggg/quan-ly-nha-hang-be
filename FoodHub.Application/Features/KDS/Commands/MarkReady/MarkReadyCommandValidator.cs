using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.KDS.Commands.MarkReady
{
    public class MarkReadyCommandValidator : AbstractValidator<MarkReadyCommand>
    {
        public MarkReadyCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId)
                .NotNull().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
