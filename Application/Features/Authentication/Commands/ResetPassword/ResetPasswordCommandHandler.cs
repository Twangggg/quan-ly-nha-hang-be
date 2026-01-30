using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FoodHub.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            // Find the token in database by its unique ID (TokenId)
            var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
                .Query()
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.TokenId == request.Id, cancellationToken);

            // Validate token exists in DB
            if (resetToken == null)
            {
                //return Result<string>.Failure("The link is invalid or has expired.");
                return Result<string>.Failure("Validate token exists in DB");
            }

            // Verify the plain token against the stored BCrypt hash
            var isValidToken = _passwordHasher.VerifyPassword(request.Token, resetToken.TokenHash);
            if (!isValidToken)
            {
                //return Result<string>.Failure("The link is invalid or has expired.");
                return Result<string>.Failure("Verify the plain token against the stored BCrypt hash");
            }

            // Validate token not expired
            if (resetToken.ExpiresAt < DateTimeOffset.UtcNow)
            {
                //return Result<string>.Failure("The link is invalid or has expired.");
                return Result<string>.Failure("Validate token not expired");
            }

            // Validate token not already used (one-time use)
            if (resetToken.IsUsed)
            {
                //return Result<string>.Failure("The link is invalid or has expired.");
                return Result<string>.Failure("Validate token not already used");
            }

            // All validations passed - proceed with password reset
            var employee = resetToken.Employee;

            // Hash the new password
            employee.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            // Mark this token as used
            resetToken.UsedAt = DateTimeOffset.UtcNow;

            // Invalidate all other active tokens for this employee (security measure)
            var otherActiveTokens = await _unitOfWork.Repository<PasswordResetToken>()
                .Query()
                .Where(t => t.EmployeeId == employee.EmployeeId 
                         && t.TokenId != resetToken.TokenId 
                         && t.UsedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in otherActiveTokens)
            {
                token.UsedAt = DateTimeOffset.UtcNow;
            }

            // Log the password reset
            await _unitOfWork.Repository<PasswordResetLog>().AddAsync(new PasswordResetLog
            {
                LogId = Guid.NewGuid(),
                TargetEmployeeId = employee.EmployeeId,
                PerformedByEmployeeId = employee.EmployeeId, // User reset their own password
                Reason = "ForgotPassword",
                ResetAt = DateTimeOffset.UtcNow
            });

            // Save all changes
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<string>.Success("Password reset successful. Please log in again.");
        }
    }
}
