using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public record CreateIngredientCommand(
        string Code,
        string Name,
        string Unit,
        decimal LowStockThreshold,
        decimal CurrentStock,
        decimal CostPrice,
        string? Description = null
    ) : IRequest<Result<CreateIngredientResponse>>;
}
