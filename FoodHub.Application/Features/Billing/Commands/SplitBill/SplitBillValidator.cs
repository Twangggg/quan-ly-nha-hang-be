using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;

namespace FoodHub.Application.Features.Billing.Commands.SplitBill
{
    public class SplitBillValidator : AbstractValidator<SplitBillCommand>
    {
        public SplitBillValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(
                    messageService.GetMessage(
                        MessageKeys.Common.IdRequired,
                        new { Field = "OrderId" }
                    )
                );

            RuleFor(x => x.ItemsToSplit)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Billing.SplitBillRequiresItems));

            RuleForEach(x => x.ItemsToSplit)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.OrderItemId)
                        .NotEmpty()
                        .WithMessage(
                            messageService.GetMessage(
                                MessageKeys.Common.IdRequired,
                                new { Field = "OrderItemId" }
                            )
                        );

                    item.RuleFor(x => x.QuantityToSplit)
                        .GreaterThan(0)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.OrderItem.InvalidQuantity)
                        );
                });
        }
    }
}
