using System;
using System.Collections.Generic;

namespace FoodHub.Domain.Entities
{
    public class OrderItemOptionGroup
    {
        public Guid OrderItemOptionGroupId { get; set; }
        public Guid OrderItemId { get; set; }
        public virtual OrderItem OrderItem { get; set; } = null!;

        public required string GroupNameSnapshot { get; set; }
        public required string GroupTypeSnapshot { get; set; } // SINGLE_SELECT / MULTI_SELECT / SCALE / FREE_TEXT
        public bool IsRequiredSnapshot { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<OrderItemOptionValue> OptionValues { get; set; } = new List<OrderItemOptionValue>();
    }
}
