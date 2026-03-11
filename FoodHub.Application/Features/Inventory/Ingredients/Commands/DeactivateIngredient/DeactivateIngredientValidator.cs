using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient
{
    public class DeactivateIngredientValidator : AbstractValidator<DeactivateIngredientCommand>
    {
        public DeactivateIngredientValidator(IMessageService messageService)
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Ingredient.IdRequired));
        }
    }
}
