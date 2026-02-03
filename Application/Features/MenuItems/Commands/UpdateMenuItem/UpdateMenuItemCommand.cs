using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public record UpdateMenuItemCommand(
        Guid MenuItemId,
        string Code,
        string Name,
        string ImageUrl,
        string? Description,
        Guid CategoryId,
        int Station,
        int ExpectedTime,
        decimal PriceDineIn,
        decimal? PriceTakeAway,
        decimal? Cost,
        bool IsOutOfStock
    ) : IRequest<Result<Unit>>;
}
