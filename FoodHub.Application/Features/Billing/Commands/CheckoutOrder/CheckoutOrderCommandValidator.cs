using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
    {
        public CheckoutOrderCommandValidator(IMessageService messageService)
        {
            RuleFor(v => v.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "OrderId" }));

            RuleFor(v => v)
                .Must(v => v.PaymentLines.Count > 0 || v.LegacyPaymentMethod.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "PaymentLines" }));

            When(v => v.PaymentLines.Count > 0, () =>
            {
                RuleForEach(v => v.PaymentLines).ChildRules(line =>
                {
                    line.RuleFor(l => l.PaymentMethodConfigId)
                        .NotEmpty()
                        .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "PaymentMethodConfigId" }));

                    line.RuleFor(l => l.Amount)
                        .GreaterThan(0)
                        .WithMessage(messageService.GetMessage(MessageKeys.Billing.PaymentAmountRequired));

                    line.RuleFor(l => l.AmountReceived)
                        .GreaterThan(0)
                        .When(l => l.AmountReceived.HasValue)
                        .WithMessage(messageService.GetMessage(MessageKeys.Billing.PaymentAmountRequired));
                });
            });

            When(v => v.PaymentLines.Count == 0 && v.LegacyPaymentMethod.HasValue, () =>
            {
                RuleFor(v => v.LegacyPaymentMethod!.Value)
                    .Must(method => method != PaymentMethod.QRCode)
                    .WithMessage(messageService.GetMessage(MessageKeys.Order.InvalidAction));

                RuleFor(v => v.LegacyAmountReceived)
                    .GreaterThan(0)
                    .When(v => v.LegacyAmountReceived.HasValue)
                    .WithMessage(messageService.GetMessage(MessageKeys.Billing.PaymentAmountRequired));
            });
        }
    }
}
