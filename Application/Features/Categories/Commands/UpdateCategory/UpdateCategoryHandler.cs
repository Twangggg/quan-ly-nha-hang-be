using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;


namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<UpdateCategoryResponse>>
    {
        public UpdateCategoryHandler()
        {
        }

        public async Task<Result<UpdateCategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }
    }
}
