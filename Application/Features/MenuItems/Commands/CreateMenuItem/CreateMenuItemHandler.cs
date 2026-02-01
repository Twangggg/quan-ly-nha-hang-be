using AutoMapper;
using FoodHub.Application.DTOs.MenuItems;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem
{
    public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<MenuItemDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateMenuItemHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<MenuItemDto>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement CreateMenuItem logic
            throw new NotImplementedException();
        }
    }
}
