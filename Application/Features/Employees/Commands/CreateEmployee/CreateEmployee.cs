using AutoMapper;
using FluentValidation;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployee
    {
        public record Command(
            string EmployeeCode,
            string Password,
            string FullName,
            string Email,
            EmployeeRole Role,
            EmployeeStatus Status = EmployeeStatus.Active
            ) : IRequest<Response>, IMapFrom<Employee>;
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(20);
                RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
                RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Role).IsInEnum();
            }
        }

        public record Response : IMapFrom<Employee>
        {
            public Guid EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = null!;
            public string FullName { get; set; } = null!;
            public string Role { get; set; } = null!;
            public DateTime CreatedAt { get; set; }

            public void Mapping(Profile profile)
            {
                profile.CreateMap<Employee, Response>()
                    .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()));
            }
        }

        public class Handler : IRequestHandler<Command, Response>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IMapper _mapper;
            private readonly IPasswordHasher _passwordHasher;

            public Handler(IUnitOfWork unitOfWork, IMapper mapper, IPasswordHasher passwordHasher)
            {
                _unitOfWork = unitOfWork;
                _mapper = mapper;
                _passwordHasher = passwordHasher;
            }

            public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
            {
                var employee = _mapper.Map<Employee>(request);

                employee.PasswordHash = _passwordHasher.HashPassword(request.Password);
                employee.EmployeeId = Guid.NewGuid();
                employee.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.Repository<Employee>().AddAsync(employee);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                return _mapper.Map<Response>(employee);
            }
        }
    }
}