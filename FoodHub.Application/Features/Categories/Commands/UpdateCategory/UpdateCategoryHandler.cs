using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryHandler
        : IRequestHandler<UpdateCategoryCommand, Result<UpdateCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public UpdateCategoryHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<UpdateCategoryResponse>> Handle(
            UpdateCategoryCommand request,
            CancellationToken cancellationToken
        )
        {
            var repo = _unitOfWork.Repository<Category>();

            var category = await repo.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category is null)
                return Result<UpdateCategoryResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Category.NotFound)
                );

            if (category.CategoryType != request.Type)
            {
                var menuItemRepo = _unitOfWork.Repository<MenuItem>();
                var setMenuRepo = _unitOfWork.Repository<SetMenu>();

                bool hasItems =
                    await menuItemRepo
                        .Query()
                        .AnyAsync(m => m.CategoryId == request.CategoryId, cancellationToken)
                    || await setMenuRepo
                        .Query()
                        .AnyAsync(s => s.CategoryId == request.CategoryId, cancellationToken);

                if (hasItems)
                    return Result<UpdateCategoryResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Category.CannotChangeTypeNotEmpty)
                    );
            }

            category.Name = request.Name;
            category.CategoryType = request.Type;

            await _unitOfWork.SaveChangeAsync();

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.CategoryList, cancellationToken);
            await _cacheService.RemoveAsync(
                string.Format(CacheKey.CategoryById, request.CategoryId),
                cancellationToken
            );
            await _cacheService.RemoveByPatternAsync("category:list:type:", cancellationToken);

            var response = new UpdateCategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                CodePrefix = category.CodePrefix,
                Type = (int)category.CategoryType,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
            };

            return Result<UpdateCategoryResponse>.Success(response);
        }
    }
}
