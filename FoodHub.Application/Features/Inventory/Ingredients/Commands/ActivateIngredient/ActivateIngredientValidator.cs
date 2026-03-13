using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient
{
    public class ActivateIngredientValidator : AbstractValidator<ActivateIngredientCommand>
    {
        public ActivateIngredientValidator(IMessageService messageService)
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.IdRequired));
        }
    }
}
