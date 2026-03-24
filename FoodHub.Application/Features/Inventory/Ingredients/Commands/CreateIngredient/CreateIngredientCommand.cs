using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public record CreateIngredientCommand(
        string? Code,
        string Name,
        string BaseUnit,
        decimal LowStockThreshold,
        bool UseDefaultLowStockThreshold = false,
        Guid? InventoryGroupId = null,
        string? Description = null
    ) : IRequest<Result<CreateIngredientResponse>>;
}
