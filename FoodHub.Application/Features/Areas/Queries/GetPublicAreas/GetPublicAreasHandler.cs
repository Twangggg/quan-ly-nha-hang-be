using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Areas.Queries.GetPublicAreas
{
    public class GetPublicAreasHandler : IRequestHandler<GetPublicAreasQuery, Result<List<GetPublicAreasResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetPublicAreasHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetPublicAreasResponse>>> Handle(GetPublicAreasQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = "area:public:list";

            var cachedResult = await _cacheService.GetAsync<List<GetPublicAreasResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<List<GetPublicAreasResponse>>.Success(cachedResult);
            }

            var areas = await _unitOfWork.Repository<Area>().Query()
                .AsNoTracking()
                .Where(a => a.Status == AreaStatus.Active)
                .OrderBy(a => a.Name)
                .ProjectTo<GetPublicAreasResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, areas, CacheTTL.Areas, cancellationToken);
            return Result<List<GetPublicAreasResponse>>.Success(areas);
        }
    }
}
