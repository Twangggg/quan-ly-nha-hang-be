using AutoMapper;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, CreateEmployeeResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;

        public CreateEmployeeHandler(IUnitOfWork unitOfWork, IMapper mapper, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        public async Task<CreateEmployeeResponse> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = _mapper.Map<Employee>(request);
            var testpassword = "12423424asdfasdf@@@#124";
            employee.PasswordHash = _passwordHasher.HashPassword(testpassword);
            employee.EmployeeId = Guid.NewGuid();
            employee.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Employee>().AddAsync(employee);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<CreateEmployeeResponse>(employee);
        }
    }
}
