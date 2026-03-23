using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShiftRange
{
    public class AssignShiftRangeHandler : IRequestHandler<AssignShiftRangeCommand, Result<IEnumerable<AssignShiftResponse>>>
    {
        private const double MaxDailyHours = 8.0;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ISignalRService _signalRService;
        private readonly IMapper _mapper;
        private readonly ILogger<AssignShiftRangeHandler> _logger;

        public AssignShiftRangeHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IBackgroundEmailSender emailSender,
            ISignalRService signalRService,
            IMapper mapper,
            ILogger<AssignShiftRangeHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _emailSender = emailSender;
            _signalRService = signalRService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<AssignShiftResponse>>> Handle(
            AssignShiftRangeCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<IEnumerable<AssignShiftResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            if (request.FromDate > request.ToDate)
                return Result<IEnumerable<AssignShiftResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));

            var employee = await _unitOfWork.Repository<Employee>().Query()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee is null || employee.Status != EmployeeStatus.Active)
                return Result<IEnumerable<AssignShiftResponse>>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));

            var shift = await _unitOfWork.Repository<Shift>().Query().AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null || shift.Status != ShiftStatus.Active)
                return Result<IEnumerable<AssignShiftResponse>>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            var existingInRange = await _unitOfWork.Repository<ShiftAssignment>().Query()
                .Include(a => a.Shift)
                .Where(a => a.EmployeeId == request.EmployeeId && a.AssignedDate >= request.FromDate && a.AssignedDate <= request.ToDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var toCreate = new List<ShiftAssignment>();
            double shiftHours = (shift.EndTime - shift.StartTime).TotalHours;

            for (var d = request.FromDate; d <= request.ToDate; d = d.AddDays(1))
            {
                var dayNumber = (int)d.DayOfWeek + 1;
                if (request.DaysOfWeek != null && request.DaysOfWeek.Any() && !request.DaysOfWeek.Contains(dayNumber))
                    continue;

                var daily = existingInRange.Where(a => a.AssignedDate == d).ToList();

                if (daily.Any())
                    return Result<IEnumerable<AssignShiftResponse>>.Failure($"{_messageService.GetMessage(MessageKeys.ShiftAssignment.MaxOneShiftPerDay)} ({d:dd/MM/yyyy})", ResultErrorType.Conflict);

                toCreate.Add(ShiftAssignment.Create(request.EmployeeId, request.ShiftId, d, request.Note, auditorId));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var assignment in toCreate)
                {
                    await _unitOfWork.Repository<ShiftAssignment>().AddAsync(assignment);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Summary Notification
                if (toCreate.Any())
                {
                    var assignedDates = toCreate.Select(a => a.AssignedDate).ToList();

                    await _emailSender.EnqueueShiftAssignmentRangeEmailAsync(
                        employee.Email,
                        employee.FullName,
                        shift.Name,
                        request.FromDate,
                        request.ToDate,
                        shift.StartTime,
                        shift.EndTime,
                        assignedDates,
                        employee.EmployeeId,
                        auditorId,
                        cancellationToken);

                    var signalRMessage = $"{shift.Name} (Range: {assignedDates.Count} days)";
                    await _signalRService.NotifyShiftAssignmentAsync(employee.EmployeeId, signalRMessage, request.FromDate, false);
                }

                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftAssignmentList, cancellationToken);

                foreach (var assignment in toCreate)
                {
                    assignment.Employee = employee;
                    assignment.Shift = shift;
                }

                var responses = _mapper.Map<List<AssignShiftResponse>>(toCreate);

                return Result<IEnumerable<AssignShiftResponse>>.Success(responses);
            }
            catch (DbUpdateException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                var inner = ex.InnerException?.Message ?? ex.Message;
                if (inner.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || inner.Contains("unique", StringComparison.OrdinalIgnoreCase))
                    return Result<IEnumerable<AssignShiftResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseConflict), ResultErrorType.Conflict);

                throw;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "AssignShiftRange failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
