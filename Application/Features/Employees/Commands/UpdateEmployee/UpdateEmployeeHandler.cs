using AutoMapper;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, UpdateEmployeeResponse>
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

        public async Task<UpdateEmployeeResponse> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employeeRepository = _unitOfWork.Repository<Employee>();
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                throw new NotFoundException($"Employee with ID {request.EmployeeId} was not found.");
            }

            employee.FullName = request.FullName;
            employee.Username = request.Username;
            employee.Phone = request.Phone;
            employee.Role = request.Role;
            employee.Address = request.Address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            employeeRepository.UpdateAsync(employee);

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Update,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = Guid.Parse(_currentUserService.UserId!),
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Update employee details"
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<UpdateEmployeeResponse>(employee);
        }
    }
}
