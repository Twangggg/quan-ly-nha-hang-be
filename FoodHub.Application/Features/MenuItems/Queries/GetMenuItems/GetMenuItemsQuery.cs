using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsQuery : IRequest<Result<PagedResult<GetMenuItemsResponse>>>
    {
        public PaginationParams Pagination { get; set; } = new();
    }
}
