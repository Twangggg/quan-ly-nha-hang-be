using FluentValidation;
using FoodHub.Application.Constants;
<<<<<<< HEAD
using FoodHub.Application.Interfaces;
=======
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;
>>>>>>> origin/main

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
