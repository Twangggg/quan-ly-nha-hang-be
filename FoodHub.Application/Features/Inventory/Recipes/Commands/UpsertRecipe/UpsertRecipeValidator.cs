using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;

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
                .Must(items =>
                    items == null
                    || items.Select(i => i.IngredientId).Distinct().Count() == items.Count
                )
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
