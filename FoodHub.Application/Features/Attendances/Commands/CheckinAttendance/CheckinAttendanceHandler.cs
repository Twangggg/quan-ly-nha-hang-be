using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Attendances.Commands.CheckinAttendance
{
    public class CheckinAttendanceHandler : IRequestHandler<CheckinAttendanceCommand, Result<CheckinAttendanceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<CheckinAttendanceHandler> _logger;

        public CheckinAttendanceHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<CheckinAttendanceHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CheckinAttendanceResponse>> Handle(CheckinAttendanceCommand request, CancellationToken cancellationToken)
        {
            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();
            _logger.LogInformation("User {AuditorId} is attempting to check in for attendance.", auditorId);

            var shiftAssignmentRepository = _unitOfWork.Repository<ShiftAssignment>();
            var attendanceRepository = _unitOfWork.Repository<Attendance>();

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            // Phiên bản: Giữ nguyên NULL để làm cờ nhận diện Quên Check-out (Bỏ block Check-in ngày mới)
            var existingAttendance = await attendanceRepository
                .Query()
                .Include(a => a.ShiftAssignment)
                .Where(a => a.EmployeeId == auditorId)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAttendance != null)
            {
                if (DateOnly.FromDateTime(existingAttendance.CheckInTime.Date) == today)
                {
                    _logger.LogWarning("User {AuditorId} has already checked in for today and has not checked out yet.", auditorId);
                    var errorMessage = _messageService.GetMessage(MessageKeys.Attendance.AlreadyCheckedIn);
                    return Result<CheckinAttendanceResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
                }
                else _logger.LogInformation("Skipping previous zombie attendance for user {AuditorId}. Proceeding a new checkin.", auditorId);
            }

            var shiftAssignments = await shiftAssignmentRepository
                .Query()
                .Include(csa => csa.Shift)
                .Where(csa => csa.EmployeeId == auditorId && csa.AssignedDate == today)
                .ToListAsync(cancellationToken);

            ShiftAssignment currentShiftAssignment = null;
            TimeStatus timeStatus = TimeStatus.OnTime;

            // Dùng foreach để gán biến out an toàn, tránh side-effect của biến out trong lambda Expression (FirstOrDefault)
            foreach (var csa in shiftAssignments)
            {
                if (csa.ValidateCheckin(now, out var status))
                {
                    currentShiftAssignment = csa;
                    timeStatus = status;
                    break;
                }
            }

            if (currentShiftAssignment == null)
            {
                _logger.LogWarning("No valid shift assignment found for user {AuditorId} on {Date}.", auditorId, today);

                var errorMessage = _messageService.GetMessage(MessageKeys.Attendance.NotFound);
                return Result<CheckinAttendanceResponse>.Failure(errorMessage, ResultErrorType.NotFound);
            }

            var attendance = Attendance.Checkin(
                auditorId,
                null,
                currentShiftAssignment.ShiftAssignmentId,
                currentShiftAssignment,
                now,
                timeStatus,
                auditorId
            );

            await attendanceRepository.AddAsync(attendance);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKey.AttendanceReportList, cancellationToken);

            _logger.LogInformation("User {AuditorId} successfully checked in for attendance with ID {AttendanceId}.", auditorId, attendance.AttendanceId);

            var response = _mapper.Map<CheckinAttendanceResponse>(attendance);
            return Result<CheckinAttendanceResponse>.Success(response);
        }
    }
}
