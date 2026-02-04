using AutoMapper;
using FoodHub.Application.Constants;
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
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IPasswordService _passwordService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ChangeRoleHandler> _logger;

        public ChangeRoleHandler(
            IUnitOfWork unitOfWork,
            IEmployeeServices employeeServices,
            ICurrentUserService currentUserService,
            IBackgroundEmailSender emailSender,
            IMapper mapper,
            IPasswordService passwordService,
            IMessageService messageService,
            ILogger<ChangeRoleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _employeeServices = employeeServices;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _emailSender = emailSender;
            _passwordService = passwordService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<ChangeRoleResponse>> Handle(ChangeRoleCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<ChangeRoleResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            if (request.NewRole == EmployeeRole.Manager)
            {
                return Result<ChangeRoleResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotPromoteToManager), ResultErrorType.BadRequest);

            }
            if (request.CurrentRole == request.NewRole)
            {
                return Result<ChangeRoleResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.NewRoleMustBeDifferent), ResultErrorType.BadRequest);
            }
            var oldEmployee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode && e.Role == request.CurrentRole, cancellationToken);

            if (oldEmployee == null)
            {
                return Result<ChangeRoleResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.NotFound), ResultErrorType.NotFound);
            }

            if (oldEmployee.Status != EmployeeStatus.Active)
            {
                return Result<ChangeRoleResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.NotActive), ResultErrorType.BadRequest);
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
                Reason = $"Deactivate old account for Role Change to: {request.NewRole}"
            };

            var logCreate = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Create,
                TargetId = newEmployee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = $"Create new account with Role: {request.NewRole} from old account: {oldEmployee.EmployeeCode}"
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
                        _messageService.GetMessage(MessageKeys.Common.DatabaseConflict),
                        ResultErrorType.Conflict
                    );
                }

                return Result<ChangeRoleResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.BadRequest
                );
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Role change operation was cancelled for {EmployeeCode}", request.EmployeeCode);
                return Result<ChangeRoleResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.OperationCancelled),
                    ResultErrorType.BadRequest
                );
            }

            await _emailSender.EnqueueRoleChangeEmailAsync(
                newEmployee.Email,
                newEmployee.FullName,
                oldEmployee.EmployeeCode,
                newEmployee.EmployeeCode,
                request.CurrentRole.ToString(),
                request.NewRole.ToString(),
                newEmployee.EmployeeId,
                auditorId,
                cancellationToken);

            var response = _mapper.Map<ChangeRoleResponse>(newEmployee);
            return Result<ChangeRoleResponse>.Success(response);


        }
    }
}
