using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient
{
    public class UpdateIngredientValidator : AbstractValidator<UpdateIngredientCommand>
    {
        public UpdateIngredientValidator(IMessageService messageService)
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.IdRequired));

            RuleFor(x => x)
                .Must(
                    command =>
                        command.InventoryGroupId == null
                        || command.InventoryGroupId == command.IngredientId
                )
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdMismatch));

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.CodeRequired))
                .MaximumLength(20)
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.CodeMaxLength));

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

        }
    }
}
