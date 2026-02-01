using FoodHub.Application.DTOs.MenuItems;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public record CreateMenuItemCommand(
        string Code,
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
