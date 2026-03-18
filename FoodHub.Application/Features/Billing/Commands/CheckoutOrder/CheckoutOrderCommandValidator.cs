using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
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

            RuleFor(v => v.PaymentMethod)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidFormat, new { Field = "PaymentMethod" }));

            RuleFor(v => v.AmountPaid)
                .GreaterThanOrEqualTo(0)
                .When(v => v.PaymentMethod == PaymentMethod.Cash)
                .WithMessage(messageService.GetMessage(MessageKeys.Order.InvalidQuantity));
        }
    }
}
