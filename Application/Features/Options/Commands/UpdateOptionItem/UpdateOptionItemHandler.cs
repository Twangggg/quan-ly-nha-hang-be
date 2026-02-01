using AutoMapper;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemHandler : IRequestHandler<UpdateOptionItemCommand, Result<OptionItemDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateOptionItemHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<OptionItemDto>> Handle(UpdateOptionItemCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement UpdateOptionItem logic
            throw new NotImplementedException();
        }
    }
}
