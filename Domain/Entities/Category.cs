
using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;


namespace FoodHub.Domain.Entities
{
    public class Category
    {
        public Guid CategoryId { get; set; }
        public required string Name { get; set; }
        public CategoryType CategoryType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
          public DateTime? DeletedAt { get; set; }


        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
