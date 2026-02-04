using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Orders.Commands.UpdateDraftOrder
{
    public class UpdateDraftOrderCommandValidator : AbstractValidator<UpdateDraftOrderCommand>
    {
        public UpdateDraftOrderCommandValidator()
        {
            RuleFor(o => o.OrderId).NotEmpty();
            RuleFor(o => o.Note).MaximumLength(150);
        }
    }
}
