using AutoMapper;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionGroup
{
    public class CreateOptionGroupHandler : IRequestHandler<CreateOptionGroupCommand, Result<OptionGroupDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateOptionGroupHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<OptionGroupDto>> Handle(CreateOptionGroupCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement CreateOptionGroup logic
            throw new NotImplementedException();
        }
    }
}
