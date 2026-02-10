using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<GetCategoriesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetAllCategoriesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetCategoriesResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKey.CategoryList;

            var cachedCategories = await _cacheService.GetAsync<List<GetCategoriesResponse>>(
                cacheKey,
                cancellationToken
                );

            if (cachedCategories != null)
            {
                return Result<List<GetCategoriesResponse>>.Success(cachedCategories);
            }

            var categories = await _unitOfWork.Repository<Domain.Entities.Category>()
                .Query()
                .ProjectTo<GetCategoriesResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, categories, CacheTTL.Categories, cancellationToken);
            return Result<List<GetCategoriesResponse>>.Success(categories);
        }
    }
}
