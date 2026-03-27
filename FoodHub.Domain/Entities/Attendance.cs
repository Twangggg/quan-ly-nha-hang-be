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

        public virtual Employee Employee { get; set; } = null!;
        public virtual ShiftAssignment? ShiftAssignment { get; set; }

        public static Attendance Checkin(
            Guid employeeId,
            Employee? employee,
            Guid? shiftAssignmentId,
            ShiftAssignment? shiftAssignment,
            DateTime checkInTime,
            TimeStatus checkinStatus,
            Guid auditorId)
        {
            var attendance = new Attendance
            {
                AttendanceId = Guid.NewGuid(),
                EmployeeId = (employee is null) ? employeeId : employee.EmployeeId,
                Employee = employee!,
                ShiftAssignmentId = shiftAssignmentId ?? shiftAssignment?.ShiftAssignmentId,
                ShiftAssignment = shiftAssignment,
                CheckInTime = checkInTime,

                isLate = checkinStatus == TimeStatus.Late, // Tự động set cờ đi muộn dựa vào validation
                isEarlyLeave = false, // Mặc định khi check-in chưa về sớm

                CreatedAt = DateTime.UtcNow,
                CreatedBy = auditorId,
            };

            return attendance;
        }

        public DomainResult Checkout(
            DateTime checkOutTime,
            TimeStatus checkoutStatus,
            Guid auditorId)
        {
            CheckOutTime = checkOutTime;

            isEarlyLeave = checkoutStatus == TimeStatus.TooEarly; // Tự động set cờ về sớm dựa vào validation

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;

            return DomainResult.Success();
        }
    }
}
