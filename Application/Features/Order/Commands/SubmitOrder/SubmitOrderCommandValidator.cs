using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Order.Commands.SubmitOrder
{
    public class SubmitOrderCommandValidator : AbstractValidator<SubmitOrderCommand>
    {
        public SubmitOrderCommandValidator(IMessageService message) {
            RuleFor(o => o.OrderId).NotEmpty().WithMessage(message.GetMessage(MessageKeys.Order.NotFound));
        }
    }
}
