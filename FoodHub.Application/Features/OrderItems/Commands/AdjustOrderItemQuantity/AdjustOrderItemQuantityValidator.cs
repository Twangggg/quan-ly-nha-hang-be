using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity
{
    public class AdjustOrderItemQuantityValidator : AbstractValidator<AdjustOrderItemQuantityCommand>
    {
        public AdjustOrderItemQuantityValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity));
        }
    }
}
