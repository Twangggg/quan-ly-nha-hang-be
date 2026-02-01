using AutoMapper;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItems
{
    public class GetMenuItemsHandler : IRequestHandler<GetMenuItemsQuery, Result<PagedResult<MenuItemDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetMenuItemsHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<MenuItemDto>>> Handle(GetMenuItemsQuery request, CancellationToken cancellationToken)
        {
            // TODO: Implement GetMenuItems logic with filters (SearchCode, Price Range, CategoryId)
            throw new NotImplementedException();
        }
    }
}
