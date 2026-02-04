using FoodHub.Application.Common.Models;
using FoodHub.Application.Resources;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodHub.Application.Features.Authentication.Queries.GetCurrentUser
{
    public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetCurrentUserHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            // Get EmployeeCode from JWT claims
            var employeeCode = _httpContextAccessor.HttpContext?.User.FindFirst("EmployeeCode")?.Value;

            if (string.IsNullOrEmpty(employeeCode))
            {
                return Result<CurrentUserResponse>.Failure(ErrorMessages.Unauthorized);
            }

            // Fetch employee from database
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);

            if (employee == null)
            {
                return Result<CurrentUserResponse>.Failure(Messages.EmployeeNotFound);
            }

            var response = new CurrentUserResponse
            {
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                FullName = employee.FullName
            };

            return Result<CurrentUserResponse>.Success(response);
        }
    }
}
