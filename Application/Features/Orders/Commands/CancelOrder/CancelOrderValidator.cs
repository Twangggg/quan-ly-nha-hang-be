using FluentValidation;

namespace FoodHub.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
    {
        public CancelOrderValidator()
        {
            RuleFor(co => co.OrderId).NotEmpty();
            RuleFor(co => co.Status).NotEmpty();
            RuleFor(co => co.Reason).NotEmpty();
        }
    }
}
