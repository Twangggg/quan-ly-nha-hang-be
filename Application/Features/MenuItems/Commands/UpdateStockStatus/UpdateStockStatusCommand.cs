using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateStockStatus
{
    public record UpdateStockStatusCommand(Guid MenuItemId, bool IsOutOfStock) : IRequest<Result<Unit>>;
}
