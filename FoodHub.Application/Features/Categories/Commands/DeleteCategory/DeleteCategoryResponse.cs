using System;

namespace FoodHub.Application.Features.Categories.Commands.DeleteCategory
{
    public record DeleteCategoryResponse(Guid CategoryId, DateTime? DeletedAt);
}
