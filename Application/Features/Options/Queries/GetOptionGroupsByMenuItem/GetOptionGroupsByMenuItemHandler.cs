using AutoMapper;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<OptionGroupDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetOptionGroupsByMenuItemHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<List<OptionGroupDto>>> Handle(GetOptionGroupsByMenuItemQuery request, CancellationToken cancellationToken)
        {
            // TODO: Implement GetOptionGroupsByMenuItem logic
            throw new NotImplementedException();
        }
    }
}
