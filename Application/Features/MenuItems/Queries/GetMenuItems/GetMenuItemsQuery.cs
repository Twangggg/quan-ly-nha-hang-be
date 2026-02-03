using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsQuery : IRequest<Result<PagedResult<MenuItemDto>>>
    {
        public PaginationParams Pagination { get; set; } = new();
    }
}
