using System;

namespace FoodHub.Domain.Entities
{
    public class OptionItem
    {
        public Guid OptionItemId { get; set; } = Guid.NewGuid();
        public Guid OptionGroupId { get; set; }
        public virtual OptionGroup OptionGroup { get; set; } = null!;

        public required string Label { get; set; }
        public decimal ExtraPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
