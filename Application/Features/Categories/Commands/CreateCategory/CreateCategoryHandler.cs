using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
    {
        public CreateCategoryHandler()
        {
        }

        public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
