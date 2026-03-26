using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
    {
        public CheckoutOrderCommandValidator(IMessageService messageService)
        {
            RuleFor(v => v.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "OrderId" }));

            RuleFor(v => v.PaymentLines)
                .NotEmpty()
                .WithMessage("PaymentLines is required.");

            RuleForEach(v => v.PaymentLines).ChildRules(line =>
            {
                line.RuleFor(l => l.PaymentMethodConfigId)
                    .NotEmpty()
                    .WithMessage("PaymentMethodConfigId is required.");

                line.RuleFor(l => l.Amount)
                    .GreaterThan(0)
                    .WithMessage("Amount must be greater than 0.");
            });
        }
    }
}
