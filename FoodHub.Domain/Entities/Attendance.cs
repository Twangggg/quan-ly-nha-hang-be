using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class Attendance : BaseEntity
    {
        public Guid AttendanceId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftAssignmentId { get; set; } // FK tới ca được gán (có thể null nếu là làm ngoài giờ)
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Note { get; set; }
        public Boolean? isLate { get; set; }
        public Boolean? isEarlyLeave { get; set; }
        public Boolean? isMissCheckOut { get; set; }
        public virtual Employee Employee { get; set; } = null!;
        public virtual ShiftAssignment? ShiftAssignment { get; set; }

    }
}
