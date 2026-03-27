using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeByIdHandler
        : IRequestHandler<GetEmployeeByIdQuery, Result<GetEmployeeByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetEmployeeByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<GetEmployeeByIdResponse>> Handle(
            GetEmployeeByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var cacheKey = string.Format(CacheKey.EmployeeById, request.Id);

            var cachedEmployee = await _cacheService.GetAsync<GetEmployeeByIdResponse>(
                cacheKey,
                cancellationToken
            );

            if (cachedEmployee != null)
            {
                return Result<GetEmployeeByIdResponse>.Success(cachedEmployee);
            }
            var query = _unitOfWork.Repository<Employee>().Query();
            var response = await query
                .Where(e => e.EmployeeId == request.Id)
                .ProjectTo<GetEmployeeByIdResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (response == null)
            {
                return Result<GetEmployeeByIdResponse>.NotFound(
                    $"Employee with ID {request.Id} was not found."
                );
            }

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Employees, cancellationToken);
            return Result<GetEmployeeByIdResponse>.Success(response);
        }
    }
}
