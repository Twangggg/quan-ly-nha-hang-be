using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand, Result<DeleteEmployeeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public DeleteEmployeeHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<DeleteEmployeeResponse>> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employeeRepository = _unitOfWork.Repository<Employee>();

            var employee = employeeRepository.Query().FirstOrDefault
                (e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
            {
                return Result<DeleteEmployeeResponse>.NotFound("This employee does not exist.");
            }

            employee.Status = EmployeeStatus.Inactive;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.DeleteAt = DateTime.UtcNow;

            employeeRepository.Update(employee);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<DeleteEmployeeResponse>.Failure("Current user identity is missing or invalid.", ResultErrorType.Unauthorized);
            }

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Deactivate,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = auditorId,
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Deactivate employee (Soft Delete)"
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = _mapper.Map<DeleteEmployeeResponse>(employee);
            return Result<DeleteEmployeeResponse>.Success(response);
        }
    }
}
