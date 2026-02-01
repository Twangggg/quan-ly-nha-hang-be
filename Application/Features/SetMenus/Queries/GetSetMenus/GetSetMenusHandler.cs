using AutoMapper;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenus
{
    public class GetSetMenusHandler : IRequestHandler<GetSetMenusQuery, Result<PagedResult<SetMenuDto>>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetSetMenusHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<SetMenuDto>>> Handle(GetSetMenusQuery request, CancellationToken cancellationToken)
        {
            // TODO: Implement GetSetMenus logic with pagination
            throw new NotImplementedException();
        }
    }
}
