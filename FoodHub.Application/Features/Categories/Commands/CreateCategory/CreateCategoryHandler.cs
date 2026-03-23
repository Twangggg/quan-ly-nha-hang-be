using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public CreateCategoryHandler(IUnitOfWork unitOfWork, ICacheService cacheService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<CreateCategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Domain.Entities.Category>();

            // Check if CodePrefix exists
            var exists = await repo.AnyAsync(x => x.CodePrefix == request.CodePrefix);
            if (exists)
            {
                return Result<CreateCategoryResponse>.Failure(
                    _messageService.GetMessage("Category.CodePrefixExists"),
                    ResultErrorType.Conflict);
            }

            var category = new Domain.Entities.Category
            {
                CategoryId = Guid.NewGuid(),
                Name = request.Name,
                CodePrefix = request.CodePrefix,
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
                CodePrefix = category.CodePrefix,
                Type = (int)category.CategoryType
            };

            return Result<CreateCategoryResponse>.Success(response);
        }
    }
}
