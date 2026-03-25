using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
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
    }
}
