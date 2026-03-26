using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignmentById
{
    /// <summary>
    /// Thông tin chi tiết về một phân công ca.
    /// </summary>
    public class GetShiftAssignmentByIdResponse : IMapFrom<ShiftAssignment>
    {
        public Guid ShiftAssignmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateOnly AssignedDate { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<ShiftAssignment, GetShiftAssignmentByIdResponse>()
                .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s => s.Employee.FullName))
                .ForMember(d => d.ShiftName, opt => opt.MapFrom(s => s.Shift.Name))
                .ForMember(d => d.StartTime, opt => opt.MapFrom(s => s.Shift.StartTime))
                .ForMember(d => d.EndTime, opt => opt.MapFrom(s => s.Shift.EndTime));
        }
    }
}
