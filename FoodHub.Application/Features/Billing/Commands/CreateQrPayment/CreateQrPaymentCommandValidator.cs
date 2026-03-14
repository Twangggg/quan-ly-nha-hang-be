using FluentValidation;

namespace FoodHub.Application.Features.Billing.Commands.CreateQrPayment
{
    public class CreateQrPaymentCommandValidator : AbstractValidator<CreateQrPaymentCommand>
    {
        public CreateQrPaymentCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("OrderId cannot be empty.");
        }
    }
}
