using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Orders.Commands.UpdateOrder
{
    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        public UpdateOrderCommandValidator()
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.Note).MaximumLength(150);
        }
    }
}
