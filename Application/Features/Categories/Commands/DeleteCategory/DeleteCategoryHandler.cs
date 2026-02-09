using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result<DeleteCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public DeleteCategoryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<DeleteCategoryResponse>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Category>();

            var category = await repo.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category is null) return Result<DeleteCategoryResponse>.NotFound(_messageService.GetMessage(MessageKeys.Category.NotFound));

            category.DeletedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteCategoryResponse>.Success(new DeleteCategoryResponse(category.CategoryId, category.DeletedAt));
        }
    }
}
