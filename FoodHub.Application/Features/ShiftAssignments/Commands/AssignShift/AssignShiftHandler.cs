using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift
{
    public class AssignShiftHandler : IRequestHandler<AssignShiftCommand, Result<AssignShiftResponse>>
    {
        private const double _MaxDailyHours = 8.0;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<AssignShiftHandler> _logger;

        public AssignShiftHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IBackgroundEmailSender emailSender,
            ISignalRService signalRService,
            ILogger<AssignShiftHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _emailSender = emailSender;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<AssignShiftResponse>> Handle(
            AssignShiftCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Bắt đầu gán ca {ShiftId} cho nhân viên {EmployeeId} vào ngày {Date}", 
                request.ShiftId, request.EmployeeId, request.AssignedDate);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                _logger.LogWarning("Không xác định được ID người dùng hiện tại khi gán ca");
                return Result<AssignShiftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var employee = await _unitOfWork.Repository<Employee>().Query()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee is null)
                return Result<AssignShiftResponse>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));

            var shift = await _unitOfWork.Repository<Shift>().Query().AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
                return Result<AssignShiftResponse>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            if (shift.Status != ShiftStatus.Active)
                return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.ShiftNotActive));

            var existing = await _unitOfWork.Repository<ShiftAssignment>().Query()
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == request.EmployeeId && a.AssignedDate == request.AssignedDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (existing.Any())
                return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.MaxOneShiftPerDay), ResultErrorType.Conflict);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var assignment = ShiftAssignment.Create(request.EmployeeId, request.ShiftId, request.AssignedDate, request.Note, auditorId);
                await _unitOfWork.Repository<ShiftAssignment>().AddAsync(assignment);

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Side effects
                await _emailSender.EnqueueShiftAssignmentEmailAsync(employee.Email, employee.FullName, shift.Name, assignment.AssignedDate, shift.StartTime, shift.EndTime, false, employee.EmployeeId, auditorId, cancellationToken);
                await _signalRService.NotifyShiftAssignmentAsync(employee.EmployeeId, shift.Name, assignment.AssignedDate, false);

                await _unitOfWork.CommitTransactionAsync();
                _logger.LogInformation("Đã gán thành công ca {ShiftId} cho nhân viên {EmployeeId}", request.ShiftId, request.EmployeeId);
                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftAssignmentList, cancellationToken);

                assignment.Employee = employee;
                assignment.Shift = shift;

                return Result<AssignShiftResponse>.Success(_mapper.Map<AssignShiftResponse>(assignment));
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "AssignShift failed due to DB conflict for Employee {Id}", request.EmployeeId);

                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("unique", StringComparison.OrdinalIgnoreCase))
                    return Result<AssignShiftResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseConflict), ResultErrorType.Conflict);

                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "AssignShift failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
