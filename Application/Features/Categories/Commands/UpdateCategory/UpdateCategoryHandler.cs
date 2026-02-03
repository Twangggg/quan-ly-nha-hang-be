using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Categories;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
    {
        public UpdateCategoryHandler()
        {
        }

        public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
