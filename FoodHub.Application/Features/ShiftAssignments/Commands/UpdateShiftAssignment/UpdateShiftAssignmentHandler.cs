using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.UpdateShiftAssignment
{
    public class UpdateShiftAssignmentHandler : IRequestHandler<UpdateShiftAssignmentCommand, Result<AssignShiftResponse>>
    {
        private const double _maxDailyHours = 8.0;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpdateShiftAssignmentHandler> _logger;

        public UpdateShiftAssignmentHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<UpdateShiftAssignmentHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<AssignShiftResponse>> Handle(
            UpdateShiftAssignmentCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<AssignShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var assignment = await _unitOfWork.Repository<ShiftAssignment>().Query()
                .FirstOrDefaultAsync(a => a.ShiftAssignmentId == request.ShiftAssignmentId, cancellationToken);

            if (assignment is null)
                return Result<AssignShiftResponse>.NotFound(_messageService.GetMessage(MessageKeys.ShiftAssignment.NotFound));

            var shift = await _unitOfWork.Repository<Shift>().Query().AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
                return Result<AssignShiftResponse>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            if (shift.Status != ShiftStatus.Active)
                return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.ShiftNotActive));

            // Check overlapping and overtime (excluding current assignment if it's the same day)
            var othersOnThatDay = await _unitOfWork.Repository<ShiftAssignment>().Query()
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == assignment.EmployeeId 
                         && a.AssignedDate == request.AssignedDate
                         && a.ShiftAssignmentId != request.ShiftAssignmentId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (othersOnThatDay.Any(a => a.Shift.StartTime < shift.EndTime && a.Shift.EndTime > shift.StartTime))
                return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.OverlappingShift), ResultErrorType.Conflict);

            double dailyHours = othersOnThatDay.Sum(a => (a.Shift.EndTime - a.Shift.StartTime).TotalHours) 
                                + (shift.EndTime - shift.StartTime).TotalHours;
            
            if (dailyHours > _maxDailyHours)
                return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.OvertimeExceeded));

            try
            {
                assignment.Update(request.ShiftId, request.AssignedDate, request.Note, auditorId);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                
                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftAssignmentList, cancellationToken);

                return Result<AssignShiftResponse>.Success(_mapper.Map<AssignShiftResponse>(assignment));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateShiftAssignment failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
