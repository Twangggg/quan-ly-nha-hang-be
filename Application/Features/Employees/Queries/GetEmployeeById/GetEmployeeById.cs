using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeById
    {
        public record Query(Guid Id) : IRequest<Response>;

        public class Response : IMapFrom<Employee>
        {
            public Guid EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = null!;
            public string Username { get; set; } = null!;
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Phone { get; set; } = null!;
            public string? Address { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public string Role { get; set; } = null!;
            public string Status { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public DateTime? DeleteAt { get; set; }
            public void Mapping(Profile profile)
            {
                profile.CreateMap<Employee, Response>()
                    .ForMember(d => d.Role,
                        opt => opt.MapFrom(s => s.Role.ToString()))
                    .ForMember(d => d.Status,
                        opt => opt.MapFrom(s => s.Status.ToString()));
            }
        }

        public class Handler : IRequestHandler<Query, Response>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IMapper _mapper;

            public Handler(IUnitOfWork unitOfWork, IMapper mapper)
            {
                _unitOfWork = unitOfWork;
                _mapper = mapper;
            }

            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Employee>().Query();
                var employee = await query.FirstOrDefaultAsync(e => e.EmployeeId == request.Id);

                return _mapper.Map<Response>(employee);
            }
        }
    }
}
