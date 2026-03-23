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

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AutoAssignShift
{
    public class AutoAssignShiftHandler : IRequestHandler<AutoAssignShiftCommand, Result<List<AssignShiftResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<AutoAssignShiftHandler> _logger;

        public AutoAssignShiftHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IBackgroundEmailSender emailSender,
            ISignalRService signalRService,
            ILogger<AutoAssignShiftHandler> logger
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

        public async Task<Result<List<AssignShiftResponse>>> Handle(
            AutoAssignShiftCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<List<AssignShiftResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var employee = await _unitOfWork.Repository<Employee>().Query()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee is null)
                return Result<List<AssignShiftResponse>>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));

            if (employee.Status != EmployeeStatus.Active)
                return Result<List<AssignShiftResponse>>.Failure(_messageService.GetMessage(MessageKeys.Employee.NotActive));

            var shift = await _unitOfWork.Repository<Shift>().Query().AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId, cancellationToken);

            if (shift is null)
                return Result<List<AssignShiftResponse>>.NotFound(_messageService.GetMessage(MessageKeys.Shift.NotFound));

            if (shift.Status != ShiftStatus.Active)
                return Result<List<AssignShiftResponse>>.Failure(_messageService.GetMessage(MessageKeys.ShiftAssignment.ShiftNotActive));

            // Lấy tất cả phân công hiện có của nhân viên trong khoảng thời gian này
            var existingAssignments = await _unitOfWork.Repository<ShiftAssignment>().Query()
                .Where(a => a.EmployeeId == request.EmployeeId 
                            && a.AssignedDate >= request.FromDate 
                            && a.AssignedDate <= request.ToDate
                            && a.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var existingDates = existingAssignments.Select(a => a.AssignedDate).ToHashSet();
            
            var assignments = new List<ShiftAssignment>();
            var assignedDates = new List<DateOnly>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
                {
                    if (existingDates.Contains(date))
                    {
                        continue;
                    }

                    var assignment = ShiftAssignment.Create(request.EmployeeId, request.ShiftId, date, request.Note, auditorId);
                    await _unitOfWork.Repository<ShiftAssignment>().AddAsync(assignment);
                    
                    assignment.Employee = employee;
                    assignment.Shift = shift;
                    assignments.Add(assignment);
                    assignedDates.Add(date);
                }

                if (assignedDates.Any())
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                    // Gửi email tổng hợp
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

                    // Thông báo SignalR
                    await _signalRService.NotifyShiftAssignmentAsync(employee.EmployeeId, shift.Name, request.FromDate, false);
                    
                    await _unitOfWork.CommitTransactionAsync();
                    await _cacheService.RemoveByPatternAsync(CacheKey.ShiftAssignmentList, cancellationToken);
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }

                return Result<List<AssignShiftResponse>>.Success(_mapper.Map<List<AssignShiftResponse>>(assignments));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "AutoAssignShift failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
