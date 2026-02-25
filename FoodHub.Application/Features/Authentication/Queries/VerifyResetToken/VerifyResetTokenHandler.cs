using System.Security.Cryptography;
using System.Text;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Queries.VerifyResetToken
{
    public class VerifyResetTokenHandler : IRequestHandler<VerifyResetTokenQuery, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public VerifyResetTokenHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(VerifyResetTokenQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Result<bool>.Success(false);
            }

            // Compute hash of the incoming plain token
            var tokenHash = ComputeSha256Hash(request.Token);

            // Find the token in database by its unique hash
            var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
                .Query()
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            // Token not found
            if (resetToken == null)
            {
                return Result<bool>.Success(false);
            }

            // Token expired
            if (resetToken.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Result<bool>.Success(false);
            }

            // Token already used
            if (resetToken.IsUsed)
            {
                return Result<bool>.Success(false);
            }

            // Employee not active
            if (resetToken.Employee == null || resetToken.Employee.Status != FoodHub.Domain.Enums.EmployeeStatus.Active)
            {
                return Result<bool>.Success(false);
            }

            // All validations passed - token is valid
            return Result<bool>.Success(true);
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
