using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployees
{
    public class GetEmployeesHandler : IRequestHandler<GetEmployeesQuery, Result<PagedResult<GetEmployeesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetEmployeesHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<PagedResult<GetEmployeesResponse>>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
        {
            var queryJson = JsonSerializer.Serialize(request.Pagination);

            var cacheKey = $"{CacheKey.EmployeeList}:{queryJson.GetHashCode()}";


            var cachedResult = await _cacheService.GetAsync<PagedResult<GetEmployeesResponse>>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return Result<PagedResult<GetEmployeesResponse>>.Success(cachedResult);
            }

            var query = _unitOfWork.Repository<Employee>().Query();
            // 1. Apply Global Search
            var searchableFields = new List<Expression<Func<Employee, string?>>>
            {
                u => u.FullName,
                u => u.EmployeeCode,
                u => u.Phone,
                u => u.Email
            };
            query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

            // 2. Apply Filters
            var filterMapping = new Dictionary<string, Expression<Func<Employee, object?>>>
            {
                { "status", u => u.Status },
                { "role", u => u.Role }
            };
            query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

            // 3. Apply Multi-Sorting
            var sortMapping = new Dictionary<string, Expression<Func<Employee, object?>>>
            {
                {"username" , u => u.Username},
                {"phone", u=> u.Phone },
                {"email", u => u.Email},
                {"fullname", u => u.FullName},
                {"employeeCode", u => u.EmployeeCode },
                {"createdAt", u => u.CreatedAt }
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                u => u.EmployeeId);

            var pagedResult = await query
                .ProjectTo<GetEmployeesResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            await _cacheService.SetAsync(cacheKey, pagedResult, CacheTTL.Employees, cancellationToken);
            return Result<PagedResult<GetEmployeesResponse>>.Success(pagedResult);
        }
    }
}
