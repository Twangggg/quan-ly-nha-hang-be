using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Category : BaseEntity
    {
        public required string Name { get; set; }
        public CategoryType Type { get; set; }

        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
