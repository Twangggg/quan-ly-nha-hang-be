using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public CreateCategoryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateCategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new Domain.Entities.Category
            {
                CategoryId = Guid.NewGuid(),
                Name = request.Name,
                CategoryType = request.Type
            };

            await _unitOfWork.Repository<Domain.Entities.Category>().AddAsync(category);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.CategoryList, cancellationToken);
            await _cacheService.RemoveByPatternAsync("category:list:type:", cancellationToken);

            var response = new CreateCategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Type = (int)category.CategoryType
            };

            return Result<CreateCategoryResponse>.Success(response);
        }
    }
}
