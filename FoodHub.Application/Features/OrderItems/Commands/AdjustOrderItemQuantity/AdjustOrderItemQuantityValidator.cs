using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.OrderItems.Commands.AdjustOrderItemQuantity
{
    public class AdjustOrderItemQuantityValidator : AbstractValidator<AdjustOrderItemQuantityCommand>
    {
        public AdjustOrderItemQuantityValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderItemId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage(MessageKeys.OrderItem.InvalidQuantity);
        }
    }
}
