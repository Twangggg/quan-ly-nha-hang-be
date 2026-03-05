using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Domain.Common;
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
    }
}
