using FoodHub.Application.DTOs.MenuItems;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public record UpdateMenuItemCommand(
        Guid MenuItemId,
        string Name,
        string ImageUrl,
        string? Description,
        Guid CategoryId,
        int Station,
        int? ExpectedTime,
        decimal PriceDineIn,
        decimal? PriceTakeAway,
        decimal? Cost
    ) : IRequest<Result<MenuItemDto>>;
}
