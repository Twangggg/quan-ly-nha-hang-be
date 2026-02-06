using System;
using System.Collections.Generic;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OptionGroup
    {
        public Guid OptionGroupId { get; set; } = Guid.NewGuid();
        public Guid MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; } = null!;

        public required string Name { get; set; }
        public OptionGroupType OptionType { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
         public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public virtual ICollection<OptionItem> OptionItems { get; set; } = new List<OptionItem>();
    }
}
