using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using System.Security.Claims;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public class ResetEmployeePasswordHandler : IRequestHandler<ResetEmployeePasswordCommand, Result<ResetEmployeePasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ResetEmployeePasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
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
                return Result<ResetEmployeePasswordResponse>.Failure("Không thể xác định Manager thực hiện thao tác");
            }
            var manager = await _unitOfWork.Repository<Employee>()
                .GetByIdAsync(managerGuid);
            if (manager == null || manager.Role != EmployeeRole.Manager)
            {
                return Result<ResetEmployeePasswordResponse>.Failure("Chỉ Manager mới có quyền reset password");
            }
            // 2. Kiểm tra employee cần reset
            var employee = await _unitOfWork.Repository<Employee>()
                .GetByIdAsync(request.EmployeeId);
            if (employee == null)
            {
                return Result<ResetEmployeePasswordResponse>.Failure("Không tìm thấy nhân viên");
            }
            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<ResetEmployeePasswordResponse>.Failure("Chỉ có thể reset password cho tài khoản đang hoạt động");
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
            // 6. Gửi email thông báo
            await _emailService.SendPasswordResetByManagerEmailAsync(
                employee.Email,
                employee.FullName,
                employee.EmployeeCode,
                newPassword,
                manager.FullName,
                cancellationToken);
            // 7. Trả về response
            return Result<ResetEmployeePasswordResponse>.Success(new ResetEmployeePasswordResponse
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                NewPassword = newPassword,
                ResetAt = DateTime.UtcNow,
                Message = $"Mật khẩu đã được reset thành công. Email thông báo đã được gửi tới {employee.Email}"
            });
        }
    }
}
