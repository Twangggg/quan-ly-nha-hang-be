using AutoMapper;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Queries.GetMenuItemById
{
    public class GetMenuItemByIdHandler : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetMenuItemByIdHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<MenuItemDetailDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
        {
            // TODO: Implement GetMenuItemById logic
            throw new NotImplementedException();
        }
    }
}
