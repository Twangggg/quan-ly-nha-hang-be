using System;

namespace FoodHub.Domain.Entities
{
    public class SetMenuItem
    {
        public Guid SetMenuItemId { get; set; } = Guid.NewGuid();
        public Guid SetMenuId { get; set; }
        public virtual SetMenu SetMenu { get; set; } = null!;

        public Guid MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; } = null!;

        public int Quantity { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
