namespace FoodHub.Domain.Entities
{
    public class SetMenuItem
    {
        public Guid SetMenuItemId { get; set; }
        public int Quantity { get; set; }
        
        // Foreign keys
        public Guid SetMenuId { get; set; }
        public Guid MenuItemId { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }

        // Navigation properties
        public virtual SetMenu SetMenu { get; set; } = null!;
        public virtual MenuItem MenuItem { get; set; } = null!;
    }
}
