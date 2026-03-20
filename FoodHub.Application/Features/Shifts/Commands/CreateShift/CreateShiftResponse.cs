using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftResponse : IMapFrom<Shift>
    {
        public Guid ShiftId { get; set; }
        public required string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public ShiftStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
