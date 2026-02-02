using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public record CreateSetMenuCommand(
        string Code,
        string Name,
        decimal Price,
        List<SetMenuItemRequest> Items
    ) : IRequest<Result<SetMenuDto>>;

    public record SetMenuItemRequest(Guid MenuItemId, int Quantity);
}
