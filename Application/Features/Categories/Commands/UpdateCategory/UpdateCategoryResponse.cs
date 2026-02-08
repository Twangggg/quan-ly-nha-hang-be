using System;

namespace FoodHub.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
