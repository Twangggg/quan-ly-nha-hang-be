using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class MenuItem : BaseEntity
    {
        public Guid MenuItemId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string ImageUrl { get; set; }
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public Station Station { get; set; }
        public int ExpectedTime { get; set; } // Minutes

        public decimal PriceDineIn { get; set; }
        public decimal PriceTakeAway { get; set; }
        public decimal CostPrice { get; set; } // Internal cost

        public bool IsOutOfStock { get; set; }

        public virtual ICollection<OptionGroup> OptionGroups { get; set; } = new List<OptionGroup>();
        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
    }
}
