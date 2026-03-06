using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

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
