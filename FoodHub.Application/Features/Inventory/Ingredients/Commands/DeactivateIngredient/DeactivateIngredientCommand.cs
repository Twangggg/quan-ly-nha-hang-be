using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient
{
    public record DeactivateIngredientCommand(Guid IngredientId) : IRequest<Result<Unit>>;
}
