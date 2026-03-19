using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    /// <summary>
    /// Thực thể Ca làm việc (Shifts) - Danh mục cơ sở dùng chung cho hệ thống.
    /// </summary>
    public class Shift : BaseEntity
    {
        /// <summary>ID Định danh ca làm việc.</summary>
        public Guid ShiftId { get; set; }

        /// <summary>Tên ca làm việc (VD: Ca sáng, Ca tối, Ca hành chính).</summary>
        public string Name { get; set; } = null!;

        /// <summary>Giờ bắt đầu ca.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc ca.</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Trạng thái hoạt động (Active/Inactive).</summary>
        public ShiftStatus Status { get; set; }

        public Shift()
        {
        }

        /// <summary>
        /// Tạo một ca làm việc mới.
        /// </summary>
        /// <param name="name">Tên ca.</param>
        /// <param name="startTime">Giờ bắt đầu.</param>
        /// <param name="endTime">Giờ kết thúc.</param>
        /// <param name="createdBy">ID người tạo.</param>
        /// <returns>Đối tượng Shift mới.</returns>
        public static Shift Create(
            string name,
            TimeSpan startTime,
            TimeSpan endTime,
            Guid? createdBy = null
        )
        {
            return new Shift
            {
                ShiftId = Guid.NewGuid(),
                Name = name.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                Status = ShiftStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
            };
        }

        /// <summary>
        /// Cập nhật thông tin ca làm việc.
        /// </summary>
        /// <param name="name">Tên ca mới.</param>
        /// <param name="startTime">Giờ bắt đầu mới.</param>
        /// <param name="endTime">Giờ kết thúc mới.</param>
        /// <param name="updatedBy">ID người cập nhật.</param>
        /// <returns>Kết quả thực hiện nghiệp vụ.</returns>
        public DomainResult Update(
            string name,
            TimeSpan startTime,
            TimeSpan endTime,
            Guid? updatedBy = null
        )
        {
            if (startTime >= endTime)
                return DomainResult.Failure(DomainErrors.Shift.OverlappingTime);

            Name = name.Trim();
            StartTime = startTime;
            EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        /// <summary>
        /// Kích hoạt lại ca làm việc.
        /// </summary>
        public DomainResult Activate(Guid? updatedBy = null)
        {
            if (Status == ShiftStatus.Active)
                return DomainResult.Failure(DomainErrors.Shift.AlreadyActive);

            Status = ShiftStatus.Active;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }

        /// <summary>
        /// Vô hiệu hóa ca làm việc.
        /// </summary>
        public DomainResult Deactivate(Guid? updatedBy = null)
        {
            if (Status == ShiftStatus.Inactive)
                return DomainResult.Failure(DomainErrors.Shift.AlreadyInactive);

            Status = ShiftStatus.Inactive;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }
    }
}
