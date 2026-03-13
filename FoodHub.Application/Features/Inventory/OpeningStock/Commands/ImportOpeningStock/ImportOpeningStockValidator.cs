using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock
{
    public class ImportOpeningStockValidator : AbstractValidator<ImportOpeningStockCommand>
    {
        public ImportOpeningStockValidator(IMessageService messageService)
        {
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.OpeningStock.ItemsRequired))
                .Must(items =>
                    items.Select(x => x.IngredientId).Distinct().Count() == items.Count
                )
                .WithMessage(
                    messageService.GetMessage(MessageKeys.OpeningStock.DuplicateIngredient)
                );

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.IngredientId)
                    .NotEmpty()
                    .WithMessage(
                        messageService.GetMessage(MessageKeys.OpeningStock.IngredientIdRequired)
                    );

                item.RuleFor(x => x.Quantity)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage(messageService.GetMessage(MessageKeys.OpeningStock.QuantityMin));

                item.RuleFor(x => x.CostPrice)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.CostPrice.HasValue)
                    .WithMessage(messageService.GetMessage(MessageKeys.OpeningStock.CostPriceMin));
            });
        }
    }
}
