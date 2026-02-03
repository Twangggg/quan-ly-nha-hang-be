using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Extensions.Pagination;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, Result<PagedResult<MenuItemDto>>>
    {
        public GetMenuItemsHandler()
        {
        }

        public async Task<Result<PagedResult<MenuItemDto>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
