using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe
{
    public class UpsertRecipeValidator : AbstractValidator<UpsertRecipeCommand>
    {
        public UpsertRecipeValidator(IMessageService messageService)
        {
            RuleFor(x => x.MenuItemId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.MenuItem.NotFound));

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.StockOutReceipt.ItemsRequired))
                .Must(items => items.Select(i => i.IngredientId).Distinct().Count() == items.Count)
                .WithMessage(
                    messageService.GetMessage(MessageKeys.StockOutReceipt.DuplicateIngredient)
                );

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.IngredientId)
                        .NotEmpty()
                        .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.NotFound));

                    item.RuleFor(i => i.QuantityPerServing)
                        .GreaterThan(0)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.StockOutReceipt.QuantityMin)
                        );

                    item.RuleFor(i => i.BaseUnit)
                        .NotEmpty()
                        .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.UnitRequired))
                        .MaximumLength(20)
                        .WithMessage(
                            messageService.GetMessage(MessageKeys.Ingredient.UnitMaxLength)
                        );
                });
        }
    }
}
