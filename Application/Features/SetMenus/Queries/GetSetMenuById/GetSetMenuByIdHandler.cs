using AutoMapper;
using FoodHub.Application.DTOs.SetMenus;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById
{
    public class GetSetMenuByIdHandler : IRequestHandler<GetSetMenuByIdQuery, Result<SetMenuDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetSetMenuByIdHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<SetMenuDetailDto>> Handle(GetSetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            // TODO: Implement GetSetMenuById logic
            throw new NotImplementedException();
        }
    }
}
