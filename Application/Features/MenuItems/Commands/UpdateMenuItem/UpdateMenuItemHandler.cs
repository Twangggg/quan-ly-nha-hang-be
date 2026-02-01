using AutoMapper;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.UpdateMenuItem
{
    public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, Result<MenuItemDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateMenuItemHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<MenuItemDto>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateMenuItem logic
            throw new NotImplementedException();
        }
    }
}
