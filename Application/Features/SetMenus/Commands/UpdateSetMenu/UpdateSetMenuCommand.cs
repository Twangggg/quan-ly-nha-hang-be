using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public record UpdateSetMenuCommand(
        Guid SetMenuId,
        string Name,
        decimal Price,
        List<SetMenuItemRequest> Items
    ) : IRequest<Result<SetMenuDto>>;

    public record SetMenuItemRequest(Guid MenuItemId, int Quantity);
}
