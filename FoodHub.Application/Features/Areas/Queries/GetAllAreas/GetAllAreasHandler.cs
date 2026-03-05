using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    public class GetAllAreasHandler : IRequestHandler<GetAllAreasQuery, Result<List<GetAllAreasResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetAllAreasHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetAllAreasResponse>>> Handle(GetAllAreasQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKey.AreaList;

            var cachedResult = await _cacheService.GetAsync<List<GetAllAreasResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<List<GetAllAreasResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Area>().Query();

            var areas = await query
                .OrderBy(a => a.Name)
                .ProjectTo<GetAllAreasResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, areas, CacheTTL.Areas, cancellationToken);
            return Result<List<GetAllAreasResponse>>.Success(areas);
        }
    }

}
