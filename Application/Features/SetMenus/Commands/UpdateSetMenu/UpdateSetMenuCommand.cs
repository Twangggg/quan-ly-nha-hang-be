using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public record UpdateSetMenuCommand(
        Guid SetMenuId,
        string Name,
        decimal Price,
        List<UpdateSetMenuItemRequest> Items
    ) : IRequest<Result<UpdateSetMenuResponse>>;

    public record UpdateSetMenuItemRequest(Guid MenuItemId, int Quantity);
}
