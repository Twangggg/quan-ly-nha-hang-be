using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

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
