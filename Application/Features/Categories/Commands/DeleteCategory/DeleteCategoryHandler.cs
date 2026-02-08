using AutoMapper;
using FoodHub.Application.Common.Models;
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

        public DeleteCategoryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<DeleteCategoryResponse>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Category>();

            var category = await repo.Query()
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);

            if (category is null) return Result<DeleteCategoryResponse>.NotFound("Category is not found!");

            category.DeletedAt = DateTime.UtcNow;
            
            category.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            return Result<DeleteCategoryResponse>.Success(new DeleteCategoryResponse(category.CategoryId, category.DeletedAt));
        }
    }
}
