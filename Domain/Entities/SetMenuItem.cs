using System;

namespace FoodHub.Domain.Entities
{
    public class SetMenuItem : BaseEntity
    {
        public Guid SetMenuId { get; set; }
        public virtual SetMenu SetMenu { get; set; } = null!;

        public Guid MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
