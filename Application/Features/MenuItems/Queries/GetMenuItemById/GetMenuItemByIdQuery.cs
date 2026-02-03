using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public record GetMenuItemByIdQuery(Guid MenuItemId) : IRequest<Result<MenuItemDetailDto>>;
}
