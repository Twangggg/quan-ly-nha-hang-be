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

namespace FoodHub.Application.Features.ShiftAssignments.Commands.CancelShiftAssignment
{
    public class CancelShiftAssignmentHandler : IRequestHandler<CancelShiftAssignmentCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<CancelShiftAssignmentHandler> _logger;

        public CancelShiftAssignmentHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IBackgroundEmailSender emailSender,
            ISignalRService signalRService,
            ILogger<CancelShiftAssignmentHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _emailSender = emailSender;
            _signalRService = signalRService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            CancelShiftAssignmentCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser),
                    ResultErrorType.Unauthorized
                );
            }

            var repo = _unitOfWork.Repository<ShiftAssignment>();
            var assignment = await repo.Query()
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.ShiftAssignmentId == request.ShiftAssignmentId, cancellationToken);

            if (assignment is null)
                return Result<bool>.NotFound(_messageService.GetMessage(MessageKeys.ShiftAssignment.NotFound));

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var domainResult = assignment.Cancel(auditorId);
                if (!domainResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<bool>.Failure(_messageService.GetMessage(domainResult.ErrorCode ?? MessageKeys.Common.InternalServerError));
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Notifications
                await _emailSender.EnqueueShiftAssignmentEmailAsync(assignment.Employee.Email, assignment.Employee.FullName, assignment.Shift.Name, assignment.AssignedDate, assignment.Shift.StartTime, assignment.Shift.EndTime, true, assignment.EmployeeId, auditorId, cancellationToken);
                await _signalRService.NotifyShiftAssignmentAsync(assignment.EmployeeId, assignment.Shift.Name, assignment.AssignedDate, true);

                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync(CacheKey.ShiftAssignmentList, cancellationToken);
                await _cacheService.RemoveAsync(
                    string.Format(CacheKey.ShiftAssignmentById, assignment.ShiftAssignmentId),
                    cancellationToken
                );

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "CancelShiftAssignment failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}
