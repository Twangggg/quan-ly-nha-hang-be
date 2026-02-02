using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Result<CreateEmployeeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly IEmployeeServices _employeeServices;

        public CreateEmployeeHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPasswordService passwordService,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IEmployeeServices employeeServices)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _passwordService = passwordService;
            _emailService = emailService;
            _employeeServices = employeeServices;
        }

        public async Task<Result<CreateEmployeeResponse>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = _mapper.Map<Employee>(request);
            var randomPassword = _passwordService.GenerateRandomPassword();
            employee.PasswordHash = _passwordService.HashPassword(randomPassword);
            employee.EmployeeId = Guid.NewGuid();
            employee.CreatedAt = DateTime.UtcNow;
            employee.EmployeeCode = await _employeeServices.GenerateEmployeeCodeAsync(request.Role);

            await _unitOfWork.Repository<Employee>().AddAsync(employee);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CreateEmployeeResponse>.Failure("Current user identity is missing or invalid.", ResultErrorType.Unauthorized);
            }

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Create,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Create new employee"
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var emailSent = await _emailService.SendAccountCreationEmailAsync(
                employee.Email,
                employee.FullName,
                employee.EmployeeCode,
                employee.Role.ToString(),
                randomPassword,
                cancellationToken);

            var response = _mapper.Map<CreateEmployeeResponse>(employee);

            if (!emailSent)
            {
                return Result<CreateEmployeeResponse>.SuccessWithWarning(
                    response,
                    $"Tài khoản đã được tạo thành công nhưng email thông báo không được gửi. " +
                    $"Vui lòng thông báo trực tiếp cho nhân viên. Mã: {employee.EmployeeCode}, Mật khẩu: {randomPassword}"
                );
            }

            return Result<CreateEmployeeResponse>.Success(response);
        }
    }
}
