using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public record UpdateSetMenuCommand(
        Guid SetMenuId,
        string Name,
        Guid CategoryId,
        string? ImageUrl,
        string? Description,
        decimal Price,
        decimal CostPrice,
        List<SetMenuItemCommand> Items
    ) : IRequest<Result<UpdateSetMenuResponse>>;

    public record SetMenuItemCommand(Guid MenuItemId, int Quantity);
}

