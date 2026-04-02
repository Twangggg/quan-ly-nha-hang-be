using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;
        private readonly ILogger<LoginHandler> _logger;
        private readonly IAuditLogService _auditLogService;

        public LoginHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ITokenService tokenService,
            IRateLimiter rateLimiter,
            IMessageService messageService,
            ILogger<LoginHandler> logger,
            IAuditLogService auditLogService
        )
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<Result<LoginResponse>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Login attempt for employee code: {EmployeeCode}",
                request.EmployeeCode
            );
            var rateLimitKey = $"login_attempt:{request.EmployeeCode}";

            // Check if user is currently blocked
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                _logger.LogWarning(
                    "Login blocked for employee code: {EmployeeCode} due to rate limiting",
                    request.EmployeeCode
                );
                await _auditLogService.LogActivityAsync(AuditAction.LoginFailed, "Auth", null, null, new { code = request.EmployeeCode, reason = "RateLimited" });
                return Result<LoginResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.AccountBlocked),
                    ResultErrorType.Unauthorized
                );
            }

            // Tìm employee chỉ bằng EmployeeCode
            var employee = await _unitOfWork
                .Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(
                    e => e.EmployeeCode == request.EmployeeCode,
                    cancellationToken
                );

            if (employee == null)
            {
                _logger.LogWarning(
                    "Login failed for employee code: {EmployeeCode}. Employee not found.",
                    request.EmployeeCode
                );
                await _auditLogService.LogActivityAsync(AuditAction.LoginFailed, "Auth", null, null, new { code = request.EmployeeCode, reason = "NotFound" });
                return Result<LoginResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.InvalidCredentials),
                    ResultErrorType.Unauthorized
                );
            }

            // Kiểm tra mật khẩu
            if (!_passwordService.VerifyPassword(request.Password, employee.PasswordHash))
            {
                _logger.LogWarning(
                    "Login failed for employee code: {EmployeeCode}. Invalid password.",
                    request.EmployeeCode
                );
                // Register failed attempt (block after 5 attempts in 15 mins)
                // Register failed attempt (block after 5 attempts in 15 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 5,
                    window: TimeSpan.FromMinutes(15),
                    blockFor: TimeSpan.FromMinutes(15),
                    cancellationToken
                );

                await _auditLogService.LogActivityAsync(AuditAction.LoginFailed, "Auth", employee.EmployeeId.ToString(), null, new { code = request.EmployeeCode, reason = "InvalidPassword" });

                return Result<LoginResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.InvalidCredentials),
                    ResultErrorType.Unauthorized
                );
            }

            // All good - reset failure count
            await _rateLimiter.ResetAsync(rateLimitKey, cancellationToken);

            // Kiểm tra trạng thái account
            if (employee.Status == EmployeeStatus.Inactive)
            {
                _logger.LogWarning(
                    "Login failed for employee code: {EmployeeCode}. Account is inactive.",
                    request.EmployeeCode
                );
                await _auditLogService.LogActivityAsync(AuditAction.LoginFailed, "Auth", employee.EmployeeId.ToString(), null, new { code = request.EmployeeCode, reason = "Inactive" });
                return Result<LoginResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.AccountInactive),
                    ResultErrorType.Unauthorized
                );
            }

            // Tạo access token
            var accessToken = _tokenService.GenerateAccessToken(employee);
            var expiresIn = _tokenService.GetTokenExpirationSeconds();

            _logger.LogInformation(
                "Successfully authenticated employee code: {EmployeeCode}. Generating tokens.",
                request.EmployeeCode
            );

            var refreshToken = _tokenService.GenerateRefreshToken();
            var configDays = _tokenService.GetRefreshTokenExpirationDays();

            var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
                employee.EmployeeId,
                refreshToken,
                request.RememberMe,
                configDays
            );

            await _unitOfWork
                .Repository<Domain.Entities.RefreshToken>()
                .AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                EmployeeId = employee.EmployeeId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiresIn = (refreshTokenEntity.Expires - DateTime.UtcNow).TotalSeconds,
                EmployeeCode = employee.EmployeeCode,
                Username = employee.Username,
                FullName = employee.FullName,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                ExpiresIn = expiresIn,
            };

            await _auditLogService.LogActivityAsync(AuditAction.Login, "Auth", employee.EmployeeId.ToString());

            return Result<LoginResponse>.Success(response);
        }
    }
}
