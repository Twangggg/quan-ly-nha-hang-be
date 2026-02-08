using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using System.Security.Claims;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public class ResetEmployeePasswordHandler : IRequestHandler<ResetEmployeePasswordCommand, Result<ResetEmployeePasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMessageService _messageService;

        public ResetEmployeePasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IBackgroundEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
            _messageService = messageService;
        }

        public async Task<Result<ResetEmployeePasswordResponse>> Handle(
            ResetEmployeePasswordCommand request,
            CancellationToken cancellationToken)
        {
            var managerId = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId) || !Guid.TryParse(managerId, out var managerGuid))
            {
                return Result<ResetEmployeePasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyManager));
            }

            var manager = await _unitOfWork.Repository<Employee>()
                .GetByIdAsync(managerGuid);

            if (manager == null || manager.Role != EmployeeRole.Manager)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.ResetPassword.OnlyManagerCanReset));
            }

            var employee = await _unitOfWork.Repository<Employee>()
                .GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.NotFound));
            }

            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(_messageService.GetMessage(MessageKeys.ResetPassword.OnlyActiveEmployeeCanReset));
            }

            var newPassword = string.IsNullOrEmpty(request.NewPassword)
                ? _passwordService.GenerateRandomPassword()
                : request.NewPassword;

            employee.PasswordHash = _passwordService.HashPassword(newPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            var refreshTokens = await _unitOfWork.Repository<RefreshToken>()
                .Query()
                .Where(rt => rt.EmployeeId == employee.EmployeeId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.ResetPassword,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = managerGuid,
                Reason = request.Reason,
                Metadata = $"Reset by Manager: {manager.FullName} ({manager.EmployeeCode})",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _emailSender.EnqueuePasswordResetByManagerEmailAsync(
                employee.Email,
                employee.FullName,
                employee.EmployeeCode,
                newPassword,
                manager.FullName,
                employee.EmployeeId,
                managerGuid,
                cancellationToken);

            var response = new ResetEmployeePasswordResponse
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                NewPassword = newPassword,
                ResetAt = DateTime.UtcNow,
                Message = _messageService.GetMessage(MessageKeys.ResetPassword.SuccessWithEmail, employee.Email)
            };

            return Result<ResetEmployeePasswordResponse>.Success(response);
        }
    }
}
