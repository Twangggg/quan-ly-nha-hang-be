using AutoMapper;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler : IRequestHandler<UpdateOptionGroupCommand, Result<OptionGroupDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateOptionGroupHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<OptionGroupDto>> Handle(UpdateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateOptionGroup logic
            throw new NotImplementedException();
        }
    }
}
