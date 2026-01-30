using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPasswordHasher _passwordHasher;

        public RequestPasswordResetCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IRateLimiter rateLimiter,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _rateLimiter = rateLimiter;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
        {
            // Get IP address for rate limiting
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitKey = $"pr:{ipAddress}:{request.EmployeeCode}";

            // Check rate limiting (3 requests per 10 minutes)
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                return Result<string>.Failure("Too many password reset requests. Please try again later.");
            }

            // Find employee by EmployeeCode
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode, cancellationToken);

            // Security: Always return the same message to avoid revealing if employee exists
            const string genericMessage = "If the account was valid, the system sent a password reset link via email.";

            // Register this attempt for rate limiting
            await _rateLimiter.RegisterFailAsync(
                rateLimitKey,
                limit: 3,
                window: TimeSpan.FromMinutes(10),
                blockFor: TimeSpan.FromMinutes(10),
                cancellationToken);

            // If employee not found or not active, return generic message but don't send email
            if (employee == null || employee.Status != EmployeeStatus.Active)
            {
                return Result<string>.Success(genericMessage);
            }

            // Generate random token (32 bytes = 256 bits)
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var plainToken = Convert.ToHexString(tokenBytes).ToLower();

            // Hash the token using IPasswordHasher (BCrypt)
            var tokenHash = _passwordHasher.HashPassword(plainToken);

            // Create token expiration (15 minutes from now)
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
            var tokenId = Guid.NewGuid();

            // Save token to database
            var resetToken = new PasswordResetToken
            {
                TokenId = tokenId,
                EmployeeId = employee.EmployeeId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PasswordResetToken>().AddAsync(resetToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Generate reset link with both ID and Token for efficient BCrypt lookup
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/reset-password?id={tokenId}&token={plainToken}";

            // Send email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                employee.Email,
                resetLink,
                employee.FullName,
                cancellationToken);

            if (!emailSent)
            {
                // Log this error but still return generic message for security
                // In production, you might want to log this to monitoring system
            }

            return Result<string>.Success(genericMessage);
        }
    }
}
