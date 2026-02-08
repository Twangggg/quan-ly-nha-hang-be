using System;

namespace FoodHub.Domain.Entities
{
    public class OrderItemOptionValue
    {
        public Guid OrderItemOptionValueId { get; set; }
        public Guid OrderItemOptionGroupId { get; set; }
        public virtual OrderItemOptionGroup OptionGroup { get; set; } = null!;

        public Guid? OptionItemId { get; set; } // Nullable - trace back to original OptionItem if exists
        public virtual OptionItem? OptionItem { get; set; }

        public required string LabelSnapshot { get; set; }
        public decimal ExtraPriceSnapshot { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; } // For FREE_TEXT type or special notes

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
