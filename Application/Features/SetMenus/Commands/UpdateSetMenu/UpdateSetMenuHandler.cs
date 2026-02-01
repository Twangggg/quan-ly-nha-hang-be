using AutoMapper;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.SetMenus.Commands.UpdateSetMenu
{
    public class UpdateSetMenuHandler : IRequestHandler<UpdateSetMenuCommand, Result<SetMenuDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateSetMenuHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<SetMenuDto>> Handle(UpdateSetMenuCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateSetMenu logic with SetMenuItems
            throw new NotImplementedException();
        }
    }
}
