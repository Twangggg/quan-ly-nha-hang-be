using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.Attendances.Commands.CheckinAttendance
{
    public class CheckinAttendanceResponse : IMapFrom<Attendance>
    {
        public Guid AttendanceId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftAssignmentId { get; set; } // FK tới ca được gán (có thể null nếu là làm ngoài giờ)
        public DateTime CheckInTime { get; set; }
        public Boolean? isLate { get; set; }
        public Boolean? isEarlyLeave { get; set; }
        public Boolean? isMissCheckOut { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Attendance, CheckinAttendanceResponse>()
                .ForMember(dest => dest.Employee, opt => opt.MapFrom(src => src.Employee));
        }
    }
}
