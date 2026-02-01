using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRateLimiter _rateLimiter;

        public ChangePasswordCommandHandler(IUnitOfWork unitOfWork, IPasswordService passwordService, ICurrentUserService currentUserService, IRateLimiter rateLimiter)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
        }

        public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<string>.Failure("The user is not logged in;");
            }

            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(u => u.EmployeeId.ToString() == userId, cancellationToken);

            if (employee == null)
            {
                return Result<string>.Failure("Invalid action");
            }

            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<string>.Failure("Invalid action");
            }

            var key = $"cp:{userId}";

            // Check if user is blocked
            if (await _rateLimiter.IsBlockedAsync(key, cancellationToken))
                return Result<string>.Failure("Too many attempts. Try again later");

            // Validate new password format
            var passwordValidationError = ValidatePasswordPolicy(request.NewPassword);
            if (!string.IsNullOrEmpty(passwordValidationError))
            {
                return Result<string>.Failure(passwordValidationError);
            }

            // Validate password confirmation matches
            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result<string>.Failure("Password confirmation does not match.");
            }

            // Validate new password is different from current
            if (request.NewPassword == request.CurrentPassword)
            {
                return Result<string>.Failure("New password must be different from current password.");
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, employee.PasswordHash))
            {
                await RegisterFailedAttempt(key, cancellationToken);
                return Result<string>.Failure("The current password is incorrect.");
            }

            // All validations passed - reset fail count
            await _rateLimiter.ResetAsync(key, cancellationToken);

            // Update password
            employee.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            //Revoke existing refresh tokens
            var refreshTokens = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Where(rt => rt.EmployeeId == employee.EmployeeId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.RefreshToken>().Update(token);
            }


            //Log password reset
            await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                LogId = Guid.NewGuid(),
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = employee.EmployeeId, // self-change
                Reason = "SelfChange",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<string>.Success("Password changed successfully. Please log in again.");
        }
        private async Task RegisterFailedAttempt(string key, CancellationToken cancellationToken)
        {
            await _rateLimiter.RegisterFailAsync(
                key,
                limit: 5,
                window: TimeSpan.FromMinutes(15),
                blockFor: TimeSpan.FromMinutes(15),
                cancellationToken);
        }

        private string? ValidatePasswordPolicy(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "New password must not be empty.";

            if (password.Length < 8)
                return "Password must be at least 8 characters long.";

            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";

            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter.";

            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number.";

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                return "Password must contain at least one special character.";

            return null;
        }
    }
}