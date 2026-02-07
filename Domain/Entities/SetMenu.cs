using System;
using System.Collections.Generic;

namespace FoodHub.Domain.Entities
{
    public class SetMenu
    {
        public Guid SetMenuId { get; set; } = Guid.NewGuid();
        public required string Code { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
    }
}
