using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Category
    {
        // PK is VARCHAR(100) as requested
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public CategoryType CategoryType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
