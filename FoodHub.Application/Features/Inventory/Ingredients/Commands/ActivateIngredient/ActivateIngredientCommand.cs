using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient
{
    public record ActivateIngredientCommand(Guid IngredientId) : IRequest<Result<Unit>>;
}
