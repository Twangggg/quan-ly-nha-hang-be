using AutoMapper;
using FoodHub.Application.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Common.Constants;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Result<CreateEmployeeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IEmployeeServices _employeeServices;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public CreateEmployeeHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IPasswordService passwordService,
            ICurrentUserService currentUserService,
            IBackgroundEmailSender emailSender,
            IEmployeeServices employeeServices,
            IMessageService messageService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _employeeServices = employeeServices;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateEmployeeResponse>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<CreateEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var employeeExists = await _unitOfWork.Repository<Employee>()
                .Query()
                .AnyAsync(e => e.Email == request.Email, cancellationToken);

            if (employeeExists)
            {
                return Result<CreateEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseConflict));
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employee = _mapper.Map<Employee>(request);
                employee.EmployeeId = Guid.NewGuid();
                employee.Status = EmployeeStatus.Active;
                employee.CreatedAt = DateTime.UtcNow;
                employee.EmployeeCode = await _employeeServices.GenerateEmployeeCodeAsync(request.Role);

                var randomPassword = _passwordService.GenerateRandomPassword();
                employee.PasswordHash = _passwordService.HashPassword(randomPassword);

                await _unitOfWork.Repository<Employee>().AddAsync(employee);



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

                // Queue email asynchronously
                await _emailSender.EnqueueAccountCreationEmailAsync(
                    employee.Email,
                    employee.FullName,
                    employee.EmployeeCode,
                    employee.Role.ToString(),
                    randomPassword,
                    employee.EmployeeId, // TargetId
                    auditorId, // PerformedBy
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveByPatternAsync("employee:list", cancellationToken);

                var response = _mapper.Map<CreateEmployeeResponse>(employee);
                return Result<CreateEmployeeResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<CreateEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountCreationEmailFailed) + $" ({ex.Message})");
            }
        }
    }
}
