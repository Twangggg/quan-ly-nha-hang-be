using FoodHub.Domain.Enums;

using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift
{
    /// <summary>
    /// Kết quả gán ca làm việc.
    /// </summary>
    public class AssignShiftResponse : IMapFrom<ShiftAssignment>
    {
        public Guid ShiftAssignmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateOnly AssignedDate { get; set; }

        /// <summary>Ghi chú.</summary>
        public string? Note { get; set; }

        /// <summary>Thời điểm tạo bản ghi.</summary>
        public DateTime CreatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<ShiftAssignment, AssignShiftResponse>()
                .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee != null ? s.Employee.FullName : null))
                .ForMember(d => d.ShiftName, opt => opt.MapFrom(s => s.Shift != null ? s.Shift.Name : null))
                .ForMember(d => d.StartTime, opt => opt.MapFrom(s => s.Shift != null ? s.Shift.StartTime : default))
                .ForMember(d => d.EndTime, opt => opt.MapFrom(s => s.Shift != null ? s.Shift.EndTime : default));
        }
    }
}
