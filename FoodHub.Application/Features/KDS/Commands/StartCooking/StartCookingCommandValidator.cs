using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.KDS.Commands.StartCooking
{
    public class StartCookingCommandValidator : AbstractValidator<StartCookingCommand>
    {
        public StartCookingCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderItemId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
