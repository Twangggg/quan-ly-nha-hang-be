using System;

namespace FoodHub.Domain.Entities
{
    public class OptionItem : BaseEntity
    {
        public Guid OptionGroupId { get; set; }
        public virtual OptionGroup OptionGroup { get; set; } = null!;

        public required string Label { get; set; }
        public decimal ExtraPrice { get; set; }
    }
}
