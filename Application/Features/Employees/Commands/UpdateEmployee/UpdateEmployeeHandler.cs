using AutoMapper;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, UpdateEmployeeResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateEmployeeHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
            employee.Address = request.Address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            employeeRepository.UpdateAsync(employee);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<UpdateEmployeeResponse>(employee);
        }
    }
}
