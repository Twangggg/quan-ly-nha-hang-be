using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredientById
{
    public record GetIngredientByIdQuery(Guid IngredientId)
        : IRequest<Result<GetIngredientByIdResponse>>;
}
