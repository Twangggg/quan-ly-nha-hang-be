using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.ToggleOutOfStock
{
    public record ToggleOutOfStockCommand(Guid MenuItemId, bool IsOutOfStock) : IRequest<Result<bool>>;
}
