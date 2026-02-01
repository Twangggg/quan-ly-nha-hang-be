using AutoMapper;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.CreateSetMenu
{
    public class CreateSetMenuHandler : IRequestHandler<CreateSetMenuCommand, Result<SetMenuDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateSetMenuHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<SetMenuDto>> Handle(CreateSetMenuCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement CreateSetMenu logic with SetMenuItems
            throw new NotImplementedException();
        }
    }
}
