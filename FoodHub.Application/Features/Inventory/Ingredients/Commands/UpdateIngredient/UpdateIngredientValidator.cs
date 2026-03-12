using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

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
                .Must(command => command.RouteId == null || command.RouteId == command.IngredientId)
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

            RuleFor(x => x.Unit)
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
