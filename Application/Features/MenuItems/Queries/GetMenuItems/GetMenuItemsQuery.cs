using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public record GetMenuItemsQuery(
        string? SearchCode,
        decimal? MinPrice,
        decimal? MaxPrice,
        Guid? CategoryId,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<Result<PagedResult<MenuItemDto>>>;
}
