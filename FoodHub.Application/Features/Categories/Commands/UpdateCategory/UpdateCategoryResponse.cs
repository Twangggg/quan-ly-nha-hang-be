using System;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public CategoryType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
