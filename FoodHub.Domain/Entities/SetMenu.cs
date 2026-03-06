using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class SetMenu : BaseEntity
    {
        public Guid SetMenuId { get; set; }
        public required string Code { get; set; }
        public int ItemNumber { get; set; }
        public required string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public bool IsOutOfStock { get; set; }
        public Guid? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
    }
}
