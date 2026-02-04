using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.OrderItem.Commands.AddOrderItem
{
    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemCommandValidator(IMessageService message) 
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.MenuItemId).NotEmpty();
            RuleFor(o => o.Quantity).GreaterThan(0).WithMessage(message.GetMessage(MessageKeys.Order.InvalidQuantiry));
        }
    }
}
