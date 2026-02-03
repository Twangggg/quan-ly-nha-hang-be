using FluentValidation;

namespace FoodHub.Application.Features.Order.Commands.AddOrderItem
{
    public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
    {
        public AddOrderItemCommandValidator() 
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.MenuItemId).NotEmpty();
            RuleFor(o => o.Quantity).GreaterThan(0).WithMessage("Quantity must greater than 0");
        }
    }
}
