using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
    {
        public CreateIngredientValidator(IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.NameRequired))
                .MaximumLength(100)
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.NameMaxLength));

            RuleFor(x => x.BaseUnit)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.UnitRequired))
                .MaximumLength(20)
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.UnitMaxLength));

            RuleFor(x => x.LowStockThreshold)
                .GreaterThanOrEqualTo(0)
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.ThresholdMin));

            // Stock and cost are system-managed; creation starts at zero.
        }
    }
}
