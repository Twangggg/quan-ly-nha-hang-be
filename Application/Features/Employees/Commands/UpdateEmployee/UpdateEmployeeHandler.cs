using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, Result<UpdateEmployeeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEmployeeHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateEmployeeResponse>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employeeRepository = _unitOfWork.Repository<Employee>();
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                return Result<UpdateEmployeeResponse>.NotFound($"Employee with ID {request.EmployeeId} was not found.");
            }

            if (employee.Status == EmployeeStatus.Inactive)
            {
                return Result<UpdateEmployeeResponse>.Failure("Cannot update an inactive employee.");
            }

            employee.FullName = request.FullName;
            employee.Username = request.Username;
            employee.Phone = request.Phone;
            employee.Address = request.Address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            employeeRepository.Update(employee);

            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateEmployeeResponse>.Failure("Current user identity is missing or invalid.", ResultErrorType.Unauthorized);
            }

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
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = _mapper.Map<UpdateEmployeeResponse>(employee);
            return Result<UpdateEmployeeResponse>.Success(response);
        }
    }
}
