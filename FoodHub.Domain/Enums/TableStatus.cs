using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodHub.Domain.Enums
{
    public enum TableStatus
    {
        Available = 1,
        Occupied = 2,
        Cleaning = 3,
        Reserved = 4,
        OutOfService = 5,
    }
}
