using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeByIdHandler : IRequestHandler<GetEmployeeByIdQuery, Result<GetEmployeeByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetEmployeeByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<GetEmployeeByIdResponse>> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.EmployeeById, request.Id);

            var cachedEmployee = await _cacheService.GetAsync<GetEmployeeByIdResponse>(
                cacheKey,
                cancellationToken);

            if (cachedEmployee != null)
            {
                return Result<GetEmployeeByIdResponse>.Success(cachedEmployee);
            }
            var query = _unitOfWork.Repository<Employee>().Query();
            var employee = await query.FirstOrDefaultAsync(e => e.EmployeeId == request.Id, cancellationToken);

            if (employee == null)
            {
                return Result<GetEmployeeByIdResponse>.NotFound($"Employee with ID {request.Id} was not found.");
            }

            var response = _mapper.Map<GetEmployeeByIdResponse>(employee);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Employees, cancellationToken);
            return Result<GetEmployeeByIdResponse>.Success(response);
        }
    }
}
