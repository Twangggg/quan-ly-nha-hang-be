using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
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
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeleteEmployeeHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteEmployeeResponse>> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<DeleteEmployeeResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.CannotIdentifyUser), ResultErrorType.Unauthorized);
            }

            var employeeRepository = _unitOfWork.Repository<Employee>();

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                return Result<DeleteEmployeeResponse>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));
            }

            employee.Status = EmployeeStatus.Inactive;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.UpdatedBy = auditorId;
            employee.DeletedAt = DateTime.UtcNow;

            employeeRepository.Update(employee);

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
            await _cacheService.RemoveByPatternAsync("employee:list", cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.EmployeeById, request.EmployeeId), cancellationToken);

            var response = _mapper.Map<DeleteEmployeeResponse>(employee);
            return Result<DeleteEmployeeResponse>.Success(response);
        }
    }
}
