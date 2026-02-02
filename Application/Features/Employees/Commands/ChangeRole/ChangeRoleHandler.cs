using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        public ChangeRoleHandler(
            IUnitOfWork unitOfWork,
            IEmployeeServices employeeServices,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IMapper mapper,
            IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _employeeServices = employeeServices;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _emailService = emailService;
            _passwordService = passwordService;
        }

        public async Task<Result<ChangeRoleResponse>> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
        {
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
            oldEmployee.Email = $"{originalEmail}_role_changed_{timestamp}";
            if (!string.IsNullOrEmpty(oldEmployee.Username))
                oldEmployee.Username = $"{originalUsername}_old_{timestamp}";
            if (!string.IsNullOrEmpty(oldEmployee.Phone))
                oldEmployee.Phone = $"{originalPhone}_old_{timestamp}";

            oldEmployee.UpdatedAt = DateTime.UtcNow;

            var newEmployee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = oldEmployee.FullName,
                Email = originalEmail,
                Username = originalUsername,
                PasswordHash = oldEmployee.PasswordHash, // Keep the same password
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

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            
            await _emailService.SendRoleChangeConfirmationEmailAsync(
                newEmployee.Email,
                newEmployee.FullName,
                oldEmployee.EmployeeCode,
                newEmployee.EmployeeCode,
                request.CurrentRole.ToString(),
                request.NewRole.ToString(),
                cancellationToken);

            var response = _mapper.Map<ChangeRoleResponse>(newEmployee);
            return Result<ChangeRoleResponse>.Success(response);
        }
    }
}
