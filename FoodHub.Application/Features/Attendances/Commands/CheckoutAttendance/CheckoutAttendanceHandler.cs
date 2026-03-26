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

namespace FoodHub.Application.Features.Attendances.Commands.CheckoutAttendance
{
    public class CheckoutAttendanceHandler : IRequestHandler<CheckoutAttendanceCommand, Result<CheckoutAttendanceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<CheckoutAttendanceHandler> _logger;

        public CheckoutAttendanceHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<CheckoutAttendanceHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CheckoutAttendanceResponse>> Handle(CheckoutAttendanceCommand request, CancellationToken cancellationToken)
        {
            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();
            _logger.LogInformation("User {UserId} is attempting to check out attendance.", auditorId);

            var attendanceRepository = _unitOfWork.Repository<Attendance>();

            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);

            var attendance = await attendanceRepository
                .Query()
                .Include(a => a.ShiftAssignment)
                .ThenInclude(sa => sa.Shift)
                .Where(a => a.EmployeeId == auditorId && a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (attendance == null)
            {
                _logger.LogWarning("No active attendance record found for user {UserId}.", auditorId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Attendance.NotFound);
                return Result<CheckoutAttendanceResponse>.Failure(errorMessage, ResultErrorType.NotFound);
            }

            if (DateOnly.FromDateTime(attendance.CheckInTime) != today)
            {
                _logger.LogWarning("Invalid checkout attempt for attendance ID {AttendanceId} by user {UserId}. Check-in date does not match current date.", attendance.AttendanceId, auditorId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Attendance.InvalidCheckOutTime);
                return Result<CheckoutAttendanceResponse>.Failure(errorMessage);
            }

            // Đảm bảo dữ liệu ca làm việc tồn tại (luôn có theo config hệ thống)
            if (attendance.ShiftAssignment == null)
            {
                _logger.LogWarning("Attendance ID {AttendanceId} is missing ShiftAssignment.", attendance.AttendanceId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Attendance.NotFound);
                return Result<CheckoutAttendanceResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
            }

            // Gọi logic validation từ Entity
            attendance.ShiftAssignment.ValidateCheckout(now, out var status);
            attendance.Checkout(now, status, auditorId);

            attendanceRepository.Update(attendance);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _cacheService.RemoveAsync(CacheKey.AttendanceReportList, cancellationToken);

            _logger.LogInformation("User {UserId} successfully checked out attendance with ID {AttendanceId}.", auditorId, attendance.AttendanceId);

            var response = _mapper.Map<CheckoutAttendanceResponse>(attendance);
            return Result<CheckoutAttendanceResponse>.Success(response);
        }
    }
}
