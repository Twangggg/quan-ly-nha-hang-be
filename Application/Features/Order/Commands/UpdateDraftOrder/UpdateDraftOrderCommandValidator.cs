using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Order.Commands.UpdateDraftOrder
{
    public class UpdateDraftOrderCommandValidator : AbstractValidator<UpdateDraftOrderCommand>
    {
        public UpdateDraftOrderCommandValidator()
        {
            RuleFor(o => o.OrderItemId).NotEmpty();
            RuleFor(o => o.Note).MaximumLength(150);
        }
    }
}
