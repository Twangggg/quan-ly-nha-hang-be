using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public enum TimeStatus
    {
        OnTime,     // Điểm danh đúng giờ (trong khoảng cho phép)
        Late,       // Điểm danh muộn
        TooEarly,   // Điểm danh quá sớm (chưa đến giờ mở check-in)
        TooLate     // Điểm danh quá muộn (đã hết ca)
    }

    /// <summary>
    /// Thực thể Phân công ca làm việc - gán một ca cụ thể cho nhân viên vào một ngày xác định.
    /// </summary>
    public class ShiftAssignment : BaseEntity
    {
        /// <summary>ID định danh phân công.</summary>
        public Guid ShiftAssignmentId { get; set; }

        /// <summary>ID nhân viên được phân công.</summary>
        public Guid EmployeeId { get; set; }

        /// <summary>ID ca làm việc được gán.</summary>
        public Guid ShiftId { get; set; }

        /// <summary>Ngày làm việc cụ thể.</summary>
        public DateOnly AssignedDate { get; set; }

        /// <summary>Ghi chú thêm (không bắt buộc).</summary>
        public string? Note { get; set; }

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual Shift Shift { get; set; } = null!;

        public ShiftAssignment() { }

        /// <summary>
        /// Tạo một phân công ca mới.
        /// </summary>
        public static ShiftAssignment Create(
            Guid employeeId,
            Guid shiftId,
            DateOnly assignedDate,
            string? note = null,
            Guid? createdBy = null)
        {
            return new ShiftAssignment
            {
                ShiftAssignmentId = Guid.NewGuid(),
                EmployeeId = employeeId,
                ShiftId = shiftId,
                AssignedDate = assignedDate,
                Note = note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }

        /// <summary>
        /// Cập nhật thông tin phân công.
        /// </summary>
        public void Update(
            Guid shiftId,
            DateOnly assignedDate,
            string? note = null,
            Guid? updatedBy = null)
        {
            ShiftId = shiftId;
            AssignedDate = assignedDate;
            Note = note?.Trim();
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// Hủy phân công (soft-delete).
        /// </summary>
        public DomainResult Cancel(Guid? cancelledBy = null)
        {
            if (DeletedAt.HasValue)
                return DomainResult.Failure(DomainErrors.ShiftAssignment.NotFound);

            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = cancelledBy;

            return DomainResult.Success();
        }

        /// <summary>
        /// Xác thực thời gian check-in của nhân viên so với lịch phân công.
        /// </summary>
        public bool ValidateCheckin(DateTime checkinTime, out TimeStatus checkinStatus)
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

            // Lấy thời điểm bắt đầu và kết thúc CỦA CA LÀM VIỆC THEO NGÀY PHÂN CÔNG (đã được bọc lại thành giờ UTC để so sánh)
            var shiftStartDateTimeUtc = Shift.GetStartTime(AssignedDate, tzInfo); 
            var shiftEndDateTimeUtc = Shift.GetEndTime(AssignedDate, tzInfo);

            // Quy tắc: Cho phép check-in trước 30 phút, được coi là đúng giờ nếu check-in muộn không quá 5 phút
            var allowEarlyTime = shiftStartDateTimeUtc.AddMinutes(-30);
            var maxOnTime = shiftStartDateTimeUtc.AddMinutes(5);

            // Nếu check-in trước 30 phút trước giờ bắt đầu ca, coi là quá sớm
            if (checkinTime < allowEarlyTime)
            {
                checkinStatus = TimeStatus.TooEarly;
                return false;
            }

            // Nếu check-in muộn hơn 5 phút sau giờ bắt đầu ca, coi là muộn
            if (maxOnTime <= checkinTime  && checkinTime <= shiftEndDateTimeUtc)
            {
                checkinStatus = TimeStatus.Late;
                return true;
            }

            // Nếu check-in muộn hơn giờ kết thúc ca, coi là quá muộn
            if (shiftEndDateTimeUtc < checkinTime)
            {
                checkinStatus = TimeStatus.TooLate;
                return false;
            }

            // Nếu check-in trong khoảng từ 30 phút trước đến 5 phút sau giờ bắt đầu ca, coi là đúng giờ
            checkinStatus = TimeStatus.OnTime;
            return true;
        }

        public bool ValidateCheckout(DateTime checkoutTime, out TimeStatus checkinStatus)
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var shiftEndDateTimeUtc = Shift.GetEndTime(AssignedDate, tzInfo);

            // Quy tắc: Cho phép checkout muộn đến 30 phút sau giờ kết thúc ca làm việc
            var allowLateCheckout = shiftEndDateTimeUtc.AddMinutes(30);

            // Nếu checkout trước giờ kết thúc ca, coi là quá sớm
            if (checkoutTime < shiftEndDateTimeUtc)
            {
                checkinStatus = TimeStatus.TooEarly;
                return true;
            }

            // Nếu checkout muộn hơn 30 phút sau giờ kết thúc ca, coi là quá muộn
            if (checkoutTime > allowLateCheckout)
            {
                checkinStatus = TimeStatus.TooLate;
                return false;
            }

            // Nếu checkout trong khoảng từ giờ kết thúc ca đến 30 phút sau, coi là đúng giờ
            checkinStatus = TimeStatus.OnTime;
            return true;
        }
    }
}
