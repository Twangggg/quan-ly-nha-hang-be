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

        public CreateEmployeeHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPasswordService passwordService,
            ICurrentUserService currentUserService,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _passwordService = passwordService;
            _emailService = emailService;
        }

        public async Task<Result<CreateEmployeeResponse>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = _mapper.Map<Employee>(request);
            var randomPassword = _passwordService.GenerateRandomPassword();
            employee.PasswordHash = _passwordService.HashPassword(randomPassword);
            employee.EmployeeId = Guid.NewGuid();
            employee.CreatedAt = DateTime.UtcNow;

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

            await _emailService.SendEmailAsync(employee.Email,
                "Chào mừng đến FoodHub - Thông tin tài khoản",
                $"<h2>Xin chào {employee.FullName}</h2>" +
                $"<p>Tài khoản của bạn đã được tạo.</p>" +
                $"<p><strong>Employee Code:</strong> {employee.EmployeeCode}</p>" +
                $"<p><strong>Mật khẩu tạm thời:</strong> {randomPassword}</p>" +
                $"<p>Vui lòng đổi mật khẩu ngay khi đăng nhập lần đầu.</p>",
                cancellationToken);

            var response = _mapper.Map<CreateEmployeeResponse>(employee);
            return Result<CreateEmployeeResponse>.Success(response);
        }
    }
}
