using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Employees.Commands.ChangeRole
{
    public class ChangeRoleHandler : IRequestHandler<ChangeRoleCommand, Result<ChangeRoleResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmployeeServices _employeeServices;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<ChangeRoleHandler> _logger;

        public ChangeRoleHandler(
            IUnitOfWork unitOfWork,
            IEmployeeServices employeeServices,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IMapper mapper,
            IPasswordService passwordService,
            ILogger<ChangeRoleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _employeeServices = employeeServices;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _emailService = emailService;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<Result<ChangeRoleResponse>> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
        {
            if (request.NewRole == EmployeeRole.Manager)
            {
                return Result<ChangeRoleResponse>.Failure("Bạn không thể cho người khác thành quản lí", ResultErrorType.BadRequest);

            }
            if (request.CurrentRole == request.NewRole)
            {
                return Result<ChangeRoleResponse>.Failure("Vị trí mới phải khác vị trí hiện tại", ResultErrorType.BadRequest);
            }
            var oldEmployee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode && e.Role == request.CurrentRole, cancellationToken);

            if (oldEmployee == null)
            {
                return Result<ChangeRoleResponse>.Failure("Không tìm thấy nhân viên với mã và role hiện tại.", ResultErrorType.NotFound);
            }

            if (oldEmployee.Status != EmployeeStatus.Active)
            {
                return Result<ChangeRoleResponse>.Failure("Nhân viên hiện tại không còn hoạt động.", ResultErrorType.BadRequest);
            }

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ChangeRoleResponse>.Failure("Không xác định được người thực hiện thao tác.", ResultErrorType.Unauthorized);
            }

            var timestamp = DateTime.UtcNow.Ticks;
            var originalEmail = oldEmployee.Email;
            var originalUsername = oldEmployee.Username;
            var originalPhone = oldEmployee.Phone;

            oldEmployee.Status = EmployeeStatus.Inactive;

            var suffix = $"_old_{timestamp}";
            if (originalEmail.Length + suffix.Length > 150)
            {
                oldEmployee.Email = originalEmail.Substring(0, 150 - suffix.Length) + suffix;
            }
            else
            {
                oldEmployee.Email = originalEmail + suffix;
            }

            oldEmployee.Username = null;
            oldEmployee.Phone = null;

            oldEmployee.UpdatedAt = DateTime.UtcNow;

            var newEmployee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = oldEmployee.FullName,
                Email = originalEmail,
                Username = originalUsername,
                PasswordHash = oldEmployee.PasswordHash,
                Phone = originalPhone,
                Address = oldEmployee.Address,
                DateOfBirth = oldEmployee.DateOfBirth,
                Role = request.NewRole,
                Status = EmployeeStatus.Active,
                CreatedAt = DateTime.UtcNow,
                EmployeeCode = await _employeeServices.GenerateEmployeeCodeAsync(request.NewRole)
            };

            await _unitOfWork.Repository<Employee>().AddAsync(newEmployee);

            var logDeactivate = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Update,
                TargetId = oldEmployee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = $"Vô hiệu hóa account cũ để đổi sang Role mới: {request.NewRole}"
            };

            var logCreate = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Create,
                TargetId = newEmployee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = $"Tạo account mới với Role: {request.NewRole} từ account cũ: {oldEmployee.EmployeeCode}"
            };

            await _unitOfWork.Repository<AuditLog>().AddAsync(logDeactivate);
            await _unitOfWork.Repository<AuditLog>().AddAsync(logCreate);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while changing role for {EmployeeCode}", request.EmployeeCode);

                // Check for specific constraint violations
                var innerException = ex.InnerException?.Message ?? ex.Message;

                if (innerException.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    innerException.Contains("unique", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<ChangeRoleResponse>.Failure(
                        "Có xung đột dữ liệu trong hệ thống. Vui lòng thử lại sau vài giây.",
                        ResultErrorType.Conflict
                    );
                }

                return Result<ChangeRoleResponse>.Failure(
                    "Có lỗi xảy ra khi cập nhật dữ liệu. Vui lòng thử lại.",
                    ResultErrorType.BadRequest
                );
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Role change operation was cancelled for {EmployeeCode}", request.EmployeeCode);
                return Result<ChangeRoleResponse>.Failure(
                    "Thao tác bị hủy do timeout. Vui lòng thử lại.",
                    ResultErrorType.BadRequest
                );
            }

            var emailSent = await _emailService.SendRoleChangeConfirmationEmailAsync(
                newEmployee.Email,
                newEmployee.FullName,
                oldEmployee.EmployeeCode,
                newEmployee.EmployeeCode,
                request.CurrentRole.ToString(),
                request.NewRole.ToString(),
                cancellationToken);

            var response = _mapper.Map<ChangeRoleResponse>(newEmployee);

            if (!emailSent)
            {
                _logger.LogWarning(
                    "Role changed successfully for {EmployeeCode} but email notification failed for {Email}",
                    newEmployee.EmployeeCode,
                    newEmployee.Email
                );

                return Result<ChangeRoleResponse>.SuccessWithWarning(
                    response,
                    $"Role đã được thay đổi thành công nhưng email thông báo không được gửi. " +
                    $"Vui lòng thông báo trực tiếp cho nhân viên về mã nhân viên mới: {newEmployee.EmployeeCode}"
                );
            }

            return Result<ChangeRoleResponse>.Success(response);
        }
    }
}
