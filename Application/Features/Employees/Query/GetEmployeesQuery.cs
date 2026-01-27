using AutoMapper;
using FoodHub.Application.DTOs.Employees;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Query
{
    public record GetEmployeesQuery : IRequest<List<EmployeeDto>>;
    public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetEmployeesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<List<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
        {
            var employees = await _unitOfWork.Repository<Employee>()
                .Query()
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<EmployeeDto>>(employees);

        }
    }
}
