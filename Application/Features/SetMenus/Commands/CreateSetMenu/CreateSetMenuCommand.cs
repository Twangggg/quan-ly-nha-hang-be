using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.SetMenus;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public record CreateSetMenuCommand(
        string Code,
        string Name,
        decimal Price,
        List<CreateSetMenuItemRequest> Items
    ) : IRequest<Result<SetMenuDto>>;

    public record CreateSetMenuItemRequest(Guid MenuItemId, int Quantity);
}
