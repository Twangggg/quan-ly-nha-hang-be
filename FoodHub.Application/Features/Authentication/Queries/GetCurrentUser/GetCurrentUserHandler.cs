using FoodHub.Application.Common.Models;
using FoodHub.Application.Resources;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Authentication.Queries.GetCurrentUser
{
    public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public GetCurrentUserHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            // Get EmployeeCode from current user service
            var employeeCode = _currentUserService.EmployeeCode;

            if (string.IsNullOrEmpty(employeeCode))
            {
                return Result<CurrentUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // Fetch employee from database
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);

            if (employee == null)
            {
                return Result<CurrentUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Employee.NotFound), ResultErrorType.NotFound);
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
