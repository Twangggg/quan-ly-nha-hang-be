using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Table : BaseEntity
    {
        public Guid TableId { get; set; }
        public int TableNumber { get; set; }
        public int Capacity { get; set; }
        public Guid AreaId { get; set; }
        public virtual Area Area { get; set; } = null!;
        public TableStatus Status { get; set; } = TableStatus.Available;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public void MarkAsCleaning()
        {
            Status = TableStatus.Cleaning;
        }
        public void MarkAsAvailable()
        {
            Status = TableStatus.Available;
        }

        public string GetTableName()
        {
            if (Area != null && !string.IsNullOrEmpty(Area.CodePrefix))
            {
                return $"{Area.CodePrefix}_{TableNumber}";
            }

            return TableNumber.ToString();
        }

        public bool SetAvailable()
        {
            if (!CanAvailable())
            {
                return false; // Cannot set to available if there are active orders
            }

            Status = TableStatus.Available;
            return true;
        }

        public bool CanAvailable()
        {
            if (Orders.Any(o => o.Status == OrderStatus.Serving))
            {
                return false;
            }

            return true;
        }
    }
}
