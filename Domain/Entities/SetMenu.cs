using System;
using System.Collections.Generic;

namespace FoodHub.Domain.Entities
{
    public class SetMenu : BaseEntity
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }

        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
    }
}
