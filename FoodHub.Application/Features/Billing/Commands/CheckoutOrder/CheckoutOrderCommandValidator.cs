using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
    {
        private readonly IMessageService _messageService;

        public CheckoutOrderCommandValidator(IMessageService messageService)
        {
            _messageService = messageService;

            RuleFor(v => v.OrderId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.IdRequired, new { Field = "OrderId" }));

            RuleFor(v => v.PaymentMethod)
                .IsInEnum()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidFormat, new { Field = "PaymentMethod" }));

            RuleFor(v => v.AmountReceived)
                .GreaterThanOrEqualTo(0)
                .When(v => v.PaymentMethod == PaymentMethod.Cash)
                .WithMessage(_messageService.GetMessage(MessageKeys.Order.InvalidQuantity));
        }
    }
}
