using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient
{
    public record UpdateIngredientCommand(
        Guid IngredientId,
        string Code,
        string Name,
        string BaseUnit,
        decimal LowStockThreshold,
        string? Description,
        bool IsActive,
        bool UseDefaultLowStockThreshold = false,
        Guid? InventoryGroupId = null
    ) : IRequest<Result<UpdateIngredientResponse>>;
}
