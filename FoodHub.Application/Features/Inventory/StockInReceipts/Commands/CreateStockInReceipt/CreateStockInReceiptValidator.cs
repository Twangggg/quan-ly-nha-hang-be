using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt
{
    public class CreateStockInReceiptValidator : AbstractValidator<CreateStockInReceiptCommand>
    {
        public CreateStockInReceiptValidator(IMessageService messageService)
        {
            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Note))
                .WithMessage(messageService.GetMessage(MessageKeys.StockInReceipt.NoteMaxLength));

            RuleFor(x => x.ReceivedAt)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.ReceivedAt.HasValue)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.StockInReceipt.ReceivedAtNotInFuture)
                );

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.StockInReceipt.ItemsRequired))
                .Must(items => items.Select(x => x.IngredientId).Distinct().Count() == items.Count)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.StockInReceipt.DuplicateIngredient)
                );

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.IngredientId)
                        .NotEmpty()
                        .WithMessage(
                            messageService.GetMessage(
                                MessageKeys.StockInReceipt.IngredientIdRequired
                            )
                        );

                    item.RuleFor(x => x.Quantity)
                        .GreaterThan(0)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockInReceipt.QuantityMin)
                        );

                    item.RuleFor(x => x.UnitCost)
                        .GreaterThanOrEqualTo(0)
                        .When(x => x.UnitCost.HasValue)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockInReceipt.UnitCostMin)
                        );

                    item.RuleFor(x => x.BatchCode)
                        .MaximumLength(100)
                        .When(x => !string.IsNullOrWhiteSpace(x.BatchCode))
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockInReceipt.BatchCodeMaxLength)
                        );
                });
        }
    }
}
