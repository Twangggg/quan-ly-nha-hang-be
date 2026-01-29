using AutoMapper;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand, DeleteEmployeeResponse>
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

        public async Task<DeleteEmployeeResponse> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employeeRepository = _unitOfWork.Repository<Employee>();

            var employee = employeeRepository.Query().FirstOrDefault
                (e => e.EmployeeId == request.EmployeeId);

            if (employee == null)
            {
                throw new NotFoundException("This employee is not exist");
            }

            employee.Status = EmployeeStatus.Inactive;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.DeleteAt = DateTime.UtcNow;

            employeeRepository.UpdateAsync(employee);

            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                Action = AuditAction.Deactivate,
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = Guid.Parse(_currentUserService.UserId!),
                CreatedAt = DateTimeOffset.UtcNow,
                Reason = "Deactivate employee (Soft Delete)"
            };
            await _unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<DeleteEmployeeResponse>(employee);
        }
    }
}
