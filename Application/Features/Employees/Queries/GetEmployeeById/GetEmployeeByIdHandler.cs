using AutoMapper;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeByIdHandler : IRequestHandler<GetEmployeeByIdQuery, GetEmployeeByIdResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetEmployeeByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<GetEmployeeByIdResponse> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<Employee>().Query();
            var employee = await query.FirstOrDefaultAsync(e => e.EmployeeId == request.Id);

            if (employee == null)
            {
                throw new NotFoundException($"Employee with ID {request.Id} was not found.");
            }

            return _mapper.Map<GetEmployeeByIdResponse>(employee);
        }
    }
}
