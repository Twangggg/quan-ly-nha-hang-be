using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Area : BaseEntity
    {
        public Guid AreaId { get; set; }
        public required string Name { get; set; }
        public required string CodePrefix { get; set; }
        public AreaType Type { get; set; } = AreaType.Normal;
        public string? Description { get; set; }
        public AreaStatus Status { get; set; } = AreaStatus.Active;
        public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    }
}
