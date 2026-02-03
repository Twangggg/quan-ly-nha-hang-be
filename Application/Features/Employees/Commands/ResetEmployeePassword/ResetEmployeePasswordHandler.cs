using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using System.Security.Claims;
using FoodHub.Application.Constants;

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
            // 1. Lấy thông tin Manager đang thực hiện reset
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

            // 2. Kiểm tra employee cần reset
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

            // 3. Generate hoặc dùng password do Manager đặt
            var newPassword = string.IsNullOrEmpty(request.NewPassword)
                ? _passwordService.GenerateRandomPassword()
                : request.NewPassword;

            // 4. Hash password và cập nhật
            employee.PasswordHash = _passwordService.HashPassword(newPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            // 5. Ghi audit log
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
