using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class OrderItem
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }

        // Snapshots
        public string ItemCodeSnapshot { get; set; } = null!;
        public string ItemNameSnapshot { get; set; } = null!;
        public string StationSnapshot { get; set; } = null!;

        public OrderItemStatus Status { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }


        public string? ItemNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public virtual Order Order { get; set; } = null!;
        public virtual ICollection<OrderItemOptionGroup> OptionGroups { get; set; } = new List<OrderItemOptionGroup>();
    }
}
