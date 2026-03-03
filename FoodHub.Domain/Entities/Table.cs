using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Table : BaseEntity
    {
        public Guid TableId { get; set; }
        public required string TableNumber { get; set; }
        public TableStatus Status { get; set; }
        public int Capacity { get; set; }
        
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
