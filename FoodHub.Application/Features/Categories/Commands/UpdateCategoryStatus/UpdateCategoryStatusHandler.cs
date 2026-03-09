using System;
using System.Threading;
using System.Threading.Tasks;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategoryStatus
{
    public class UpdateCategoryStatusHandler : IRequestHandler<UpdateCategoryStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCategoryStatusHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(UpdateCategoryStatusCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Category>();
            var category = await repo.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Category.NotFound),
                    ResultErrorType.NotFound);
            }

            category.IsActive = request.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            if (Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                category.UpdatedBy = userId;
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.CategoryList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.CategoryById, request.CategoryId), cancellationToken);
            await _cacheService.RemoveByPatternAsync("category:list:type:", cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
