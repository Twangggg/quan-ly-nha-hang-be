using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICurrentUserService _currentUserService;

        public ChangePasswordCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _currentUserService = currentUserService;
        }

        public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<string>.Failure("The user is not logged in; ID not found.");
            }

            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(u => u.EmployeeId.ToString() == userId, cancellationToken);

            if (employee == null)
            {
                return Result<string>.Failure("User information not found.");
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, employee.PasswordHash))
            {
                return Result<string>.Failure("The current password is incorrect.");
            }

            employee.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            
            // Assuming UpdatedAt is a property we want to update
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<string>.Success("Password changed successfully. Please log in again.");
        }
    }
}
