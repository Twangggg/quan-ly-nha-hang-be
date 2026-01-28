using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Query
{
    public class MyProfileResponse
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public EmployeeRole Role { get; set; }
    }
    public record GetMyProfileQuery(Guid EmployeeId) : IRequest<MyProfileResponse>;

    public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetMyProfileQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MyProfileResponse> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee == null) throw new Exception("User not found");

            return _mapper.Map<MyProfileResponse>(employee);
        }
    }
}
