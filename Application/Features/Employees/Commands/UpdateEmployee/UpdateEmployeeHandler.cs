using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, Result<UpdateEmployeeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateEmployeeHandler> _logger;

        public UpdateEmployeeHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<UpdateEmployeeHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<UpdateEmployeeResponse>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var employeeRepository = _unitOfWork.Repository<Employee>();
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                return Result<UpdateEmployeeResponse>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));
            }

            if (employee.Status == EmployeeStatus.Inactive)
            {
                return Result<UpdateEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotUpdateInactive));
            }

            employee.FullName = request.FullName;
            employee.Username = request.Username;
            employee.Phone = request.Phone;
            employee.Address = request.Address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            employeeRepository.Update(employee);

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Update,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Update employee details"
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating employee {EmployeeId}", request.EmployeeId);

                var innerException = ex.InnerException?.Message ?? ex.Message;
                if (innerException.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    innerException.Contains("unique", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<UpdateEmployeeResponse>.Failure(
                         _messageService.GetMessage(MessageKeys.Common.DatabaseConflict),
                         ResultErrorType.Conflict);
                }

                return Result<UpdateEmployeeResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.BadRequest);
            }

            var response = _mapper.Map<UpdateEmployeeResponse>(employee);
            return Result<UpdateEmployeeResponse>.Success(response);
        }
    }
}
