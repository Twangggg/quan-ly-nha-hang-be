using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.KDS.Commands.CompleteCooking
{
    public class CompleteCookingCommandValidator : AbstractValidator<CompleteCookingCommand>
    {
        public CompleteCookingCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
