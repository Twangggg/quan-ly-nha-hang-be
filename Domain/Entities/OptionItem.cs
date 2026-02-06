using System;

namespace FoodHub.Domain.Entities
{
    public class OptionItem
    {
        public Guid OptionItemId { get; set; }
        public Guid OptionGroupId { get; set; }
        public virtual OptionGroup OptionGroup { get; set; } = null!;

        public required string Label { get; set; }
        public decimal ExtraPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
