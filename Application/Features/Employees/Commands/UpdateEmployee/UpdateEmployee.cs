using AutoMapper;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployee
    {
        public record Command(
            Guid EmployeeId,
            string? Username,
            string FullName,
            string? Phone,
            string? Address,
            DateOnly? DateOfBirth
            ) : IRequest<Response>;

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeId).NotEmpty();
                RuleFor(x => x.Username).MaximumLength(50);
                RuleFor(x => x.FullName).NotEmpty();
                RuleFor(x => x.Phone).MaximumLength(15);
                RuleFor(x => x.Address).MaximumLength(255);
            }
        }

        public record Response : IMapFrom<Employee>
        {
            public Guid EmployeeId { get; set; }
            public string? Username { get; set; }
            public string FullName { get; set; } = null!;
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public DateTime? UpdatedAt { get; set; }
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

                return _mapper.Map<Response>(employee);
            }
        }
    }
}