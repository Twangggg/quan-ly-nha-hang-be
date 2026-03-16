using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt
{
    public class CreateStockOutReceiptValidator : AbstractValidator<CreateStockOutReceiptCommand>
    {
        public CreateStockOutReceiptValidator(IMessageService messageService)
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.StockOutReceipt.ReasonRequired))
                .MaximumLength(500)
                .WithMessage(messageService.GetMessage(MessageKeys.StockOutReceipt.ReasonMaxLength));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.StockOutReceipt.ItemsRequired))
                .Must(items => items.Select(x => x.IngredientId).Distinct().Count() == items.Count)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.StockOutReceipt.DuplicateIngredient)
                );

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.IngredientId)
                        .NotEmpty()
                        .WithMessage(
                            messageService.GetMessage(
                                MessageKeys.StockOutReceipt.IngredientIdRequired
                            )
                        );

                    item.RuleFor(x => x.Quantity)
                        .GreaterThan(0)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockOutReceipt.QuantityMin)
                        );

                    item.RuleFor(x => x.UnitPrice)
                        .GreaterThanOrEqualTo(0)
                        .When(x => x.UnitPrice.HasValue)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockOutReceipt.UnitPriceMin)
                        );
                });
        }
    }
}
