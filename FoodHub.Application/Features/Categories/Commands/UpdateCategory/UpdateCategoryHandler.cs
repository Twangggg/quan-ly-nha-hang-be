using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using MediatR;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<UpdateCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public UpdateCategoryHandler(IUnitOfWork unitOfWork, ICacheService cacheService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<UpdateCategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Category>();

            var category = await repo.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category is null)
                return Result<UpdateCategoryResponse>.NotFound(_messageService.GetMessage(MessageKeys.Category.NotFound));

            category.Name = request.Name;
            category.CategoryType = request.Type;

            await _unitOfWork.SaveChangeAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.CategoryList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.CategoryById, request.CategoryId), cancellationToken);
            await _cacheService.RemoveByPatternAsync("category:list:type:", cancellationToken);

            var response = new UpdateCategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Type = category.CategoryType,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Result<UpdateCategoryResponse>.Success(response);
        }
    }
}
