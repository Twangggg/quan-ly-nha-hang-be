namespace FoodHub.Domain.Entities
{
    public class OptionItem
    {
        public Guid OptionItemId { get; set; }
        public string Name { get; set; } = null!;
        public decimal AdditionalPrice { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        // Foreign key
        public Guid OptionGroupId { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        // Navigation property
        public virtual OptionGroup OptionGroup { get; set; } = null!;
    }
}
