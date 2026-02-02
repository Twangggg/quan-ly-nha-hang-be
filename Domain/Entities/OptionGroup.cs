namespace FoodHub.Domain.Entities
{
    public class OptionGroup
    {
        public Guid OptionGroupId { get; set; }
        public string Name { get; set; } = null!;
        public int MinSelect { get; set; } = 0;
        public int MaxSelect { get; set; } = 1;
        public bool IsRequired { get; set; } = false;
        
        // Foreign key
        public Guid MenuItemId { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        // Navigation properties
        public virtual MenuItem MenuItem { get; set; } = null!;
        public virtual ICollection<OptionItem> OptionItems { get; set; } = new List<OptionItem>();
    }
}
