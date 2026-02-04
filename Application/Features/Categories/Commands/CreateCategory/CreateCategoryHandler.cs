using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateCategoryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<CreateCategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new Domain.Entities.Category
            {
                Name = request.Name,
                Type = request.Type
            };

            await _unitOfWork.Repository<Domain.Entities.Category>().AddAsync(category);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new CreateCategoryResponse
            {
                CategoryId = category.Id,
                Name = category.Name,
                Type = (int)category.Type
            };

            return Result<CreateCategoryResponse>.Success(response);
        }
    }
}
