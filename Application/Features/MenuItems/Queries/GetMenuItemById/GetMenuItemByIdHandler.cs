using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.MenuItems;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDetailDto>>
    {
        public GetMenuItemByIdHandler()
        {
        }

        public async Task<Result<MenuItemDetailDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
