using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Table : BaseEntity
    {
        public Guid TableId { get; set; }
        public required string TableCode { get; set; }
        public required int Capacity { get; set; }
        public required TableShape Shape { get; set; }
        public required Guid AreaId { get; set; }
        public virtual Area Area { get; set; } = null!;
        public TableStatus Status { get; set; } = TableStatus.Available;
    }
}
