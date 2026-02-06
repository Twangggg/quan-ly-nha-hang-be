using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class SetMenu
    {
        public Guid SetMenuId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public SetType SetType { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public bool IsOutOfStock { get; set; }
        public Guid? CreatedByEmployeeId { get; set; }
        public Guid? UpdatedByEmployeeId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public virtual ICollection<SetMenuItem> SetMenuItems { get; set; } = new List<SetMenuItem>();
    }
}
