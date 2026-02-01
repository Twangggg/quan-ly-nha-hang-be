using AutoMapper;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemHandler : IRequestHandler<CreateOptionItemCommand, Result<OptionItemDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateOptionItemHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<OptionItemDto>> Handle(CreateOptionItemCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement CreateOptionItem logic
            throw new NotImplementedException();
        }
    }
}
