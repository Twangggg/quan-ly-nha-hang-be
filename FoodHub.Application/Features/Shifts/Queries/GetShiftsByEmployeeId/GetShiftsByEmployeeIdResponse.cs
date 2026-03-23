using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftsByEmployeeId
{
    public class GetShiftsByEmployeeIdResponse : IMapFrom<Shift>
    {
        public Guid ShiftId { get; set; }
        public string Name { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public ShiftStatus Status { get; set; }

        public List<ShiftAssignmentResponse> ShiftAssignments { get; set; } = new List<ShiftAssignmentResponse>();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Shift, GetShiftsByEmployeeIdResponse>()
                .ForMember(dest => dest.ShiftAssignments, opt => opt.MapFrom(src => src.ShiftAssignments));
        }
    }

    public class ShiftAssignmentResponse : IMapFrom<ShiftAssignment>
    {
        public Guid ShiftAssignmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public DateOnly AssignedDate { get; set; }
        public string? Note { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<ShiftAssignment, ShiftAssignmentResponse>();
        }
    }
}
