using AutoMapper;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.DeleteEmployee
{
    public class DeleteEmployee
    {
        public record Command(Guid EmployeeId) : IRequest<Response>;
        public class Response : IMapFrom<Employee>
        {
            public Guid EmployeeId { get; set; }
            public string Role { get; set; } = null!;
            public DateTime? UpdatedAt { get; set; }
            public DateTime? DeleteAt { get; set; }
        }

        public class Handler : IRequestHandler<Command, Response>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IMapper _mapper;

            public Handler(IUnitOfWork unitOfWork, IMapper mapper)
            {
                _unitOfWork = unitOfWork;
                _mapper = mapper;
            }

            public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
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
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                return _mapper.Map<Response>(employee);
            }
        }

    }
}