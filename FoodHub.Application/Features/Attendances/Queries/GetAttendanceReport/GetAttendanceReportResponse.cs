using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    /// <summary>
    /// Thông tin báo cáo chấm công của nhân viên.
    /// </summary>
    public class GetAttendanceReportResponse : IMapFrom<Attendance>
    {
        /// <summary>
        /// ID của bản ghi chấm công.
        /// </summary>
        public Guid AttendanceId { get; set; }

        /// <summary>
        /// Ngày làm việc (được tính từ ca hoặc giờ vào).
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Tên đầy đủ của nhân viên.
        /// </summary>
        public string EmployeeName { get; set; } = null!;

        /// <summary>
        /// Tên ca làm việc (hoặc "Làm ngoài giờ").
        /// </summary>
        public string ShiftName { get; set; } = null!;

        /// <summary>
        /// Thời điểm Check-in (UTC).
        /// </summary>
        public DateTime CheckInTime { get; set; }

        /// <summary>
        /// Thời điểm Check-out (UTC, có thể null nếu chưa ra).
        /// </summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// Trạng thái chấm công (Đúng giờ, Đi trễ, Về sớm, Thiếu giờ ra).
        /// </summary>
        public string Status { get; set; } = null!;

        public DateOnly? AssignedDate { get; set; }
        public bool? isLate { get; set; }
        public bool? isEarlyLeave { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Attendance, GetAttendanceReportResponse>()
                .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee.FullName))
                .ForMember(d => d.ShiftName, opt => opt.MapFrom(s => s.ShiftAssignment != null ? s.ShiftAssignment.Shift.Name : "Làm ngoài giờ"))
                .ForMember(d => d.AssignedDate, opt => opt.MapFrom(s => s.ShiftAssignment != null ? s.ShiftAssignment.AssignedDate : (DateOnly?)null))
                .ForMember(d => d.Date, opt => opt.MapFrom(s => s.ShiftAssignment != null 
                    ? s.ShiftAssignment.AssignedDate 
                    : DateOnly.FromDateTime(s.CheckInTime.AddHours(7))))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.CheckOutTime == null 
                    ? "Thiếu giờ ra" 
                    : (s.isLate == true && s.isEarlyLeave == true) ? "Đi trễ & Về sớm"
                    : s.isLate == true ? "Đi trễ"
                    : s.isEarlyLeave == true ? "Về sớm" : "Đúng giờ"));
        }
    }
}
