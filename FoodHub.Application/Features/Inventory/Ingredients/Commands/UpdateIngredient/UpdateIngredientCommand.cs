using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient
{
    public record UpdateIngredientCommand(
        Guid IngredientId,
        string Code,
        string Name,
        string Unit,
        decimal LowStockThreshold,
        decimal CurrentStock,
        decimal CostPrice,
        string? Description,
        bool IsActive,
        Guid? RouteId = null
    ) : IRequest<Result<UpdateIngredientResponse>>;
}
